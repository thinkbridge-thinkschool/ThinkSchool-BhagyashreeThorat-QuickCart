# QuickCart — Design 

**Product:** QuickCart, a quick-commerce platform that delivers groceries and daily essentials
within minutes. A signed-in customer browses a catalog, fills a cart, and submits an order;
the system records the order, then processes it asynchronously (payment → confirm → notify).

**Architecture:** Modular monolith, clean architecture. One API deployable (`QuickCart.Api`)
plus a `QuickCart.Worker` host for background processing. Internal modules are separated by
bounded context. Dependencies point **inward** toward the Domain; contexts talk to each other
through **domain events**, not direct calls — so they can later be split into services with
minimal change.

Each bounded context owns its own domain model, events, and business rules. This keeps module
boundaries explicit, reduces coupling, and aligns the codebase with Domain-Driven Design
principles while remaining a modular monolith.

> **Day 29 update:** The originally-tiny Ordering slice grew into a working shopping flow:
> a **Catalog** (categories + products), a **Cart**, a **User** synced from Entra ID, and an
> enriched **Order**. The architecture is unchanged — these are additive contexts that follow
> the same aggregate/event patterns. Order creation is now **server-authoritative**: the owner
> comes from the authenticated token and prices come from the Catalog, never from the client.

> **Day 32 update:** The application is deployed to Azure App Service (Southeast Asia) with
> full Managed Identity authentication, Entra ID multi-tenant support, SPA hosting embedded
> in the API wwwroot, and a complete CI pipeline. The frontend Angular application, API, and
> Worker all run in production.

---

## Bounded contexts

| Context | Responsibility | Status |
|---|---|---|
| **Catalog** | Categories and products — the source of truth for what can be ordered and at what price. | ✅ Implemented |
| **Ordering** | The order lifecycle and the shopping **Cart** that feeds it (browse → cart → submit → pay → deliver/cancel). The core context. | ✅ Implemented |
| **Shared** | Cross-context building blocks and the **User** (identity synced from Entra). | ✅ Implemented |
| **Notifications** | Reacts to the `OrderCreated` integration event (email/SMS). | Planned |
| **Inventory** | Stock levels; reserves/releases stock for an order. | Future scope |
| **Payment** | Charges the customer, reports success/failure. | Future scope |

Ordering is the core context. Catalog and Shared/User support it. Notifications/Inventory/Payment
integrate asynchronously and remain future scope.

---

## Shared building blocks

- **`IDomainEvent`** — marker for something noteworthy that happened inside an aggregate.
- **`BaseEntity`** — audit + soft-delete fields reused across entities: `CreatedAtUtc`,
  `ModifiedAtUtc`, `IsDeleted`. It deliberately **does not define the primary key** — each
  entity declares its own explicit key (`OrderId`, `ProductId`, …) per the naming convention
  below, so there is no generic `Id` to collide with.
- **Explicit key names.** Every entity uses a named key (`UserId`, `CategoryId`, `ProductId`,
  `CartId`, `CartItemId`, `OrderId`, `OrderItemId`) rather than a bare `Id`.

---

## Catalog context

- **`Category`** (`CategoryId`, `CategoryName`, `Description`) — e.g. Grocery, Personal Care,
  Beverages, Snacks, Electronics, Frozen Foods, Health & Wellness, Pet Care, Bread & Bakery.
- **`Product`** (`ProductId`, `CategoryId`, `ProductName`, `Description`, `Price`, `ImageUrl`,
  `StockQuantity`, `IsAvailable`). Products belong to a category. **Image URLs only** — no
  binaries in SQL; the frontend uses Angular assets or Blob Storage URLs.

Catalog is the price authority: when an order is placed, the Ordering context resolves the
current `Price` and `ProductName` from here rather than trusting client input.

**Seeding:** `CatalogSeeder.SeedAsync` runs at API startup (idempotent — checks whether
categories exist first). It inserts 112 products across 12 categories. For SQL Server the
schema must already exist (migrations applied separately by a privileged identity); for SQLite
(local dev / integration tests) `EnsureCreatedAsync` bootstraps the database automatically.

---

## Shared / User

- **`User`** (`UserId`, `EntraObjectId`, `Email`, `DisplayName`, `PhoneNumber`).
- Users authenticate through Microsoft Entra ID. On first authenticated request the user is
  **synced/created locally** from the token's object-id and profile claims (`EntraObjectId` is
  the stable link). Orders and carts belong to the authenticated `UserId` — the API never
  accepts a customer id, name, or email in a request body.

---

## Core aggregate — `Order`

The aggregate root for the Ordering context. It owns its items, enforces its own invariants,
and records domain events instead of calling other contexts directly.

- **Root:** `Order` (`OrderId`, `UserId`, `Status`, `TotalAmount`, `CreatedAtUtc`)
- **Owned entity:** `OrderItem` (`OrderItemId`, `ProductId`, `UnitPrice`, `Quantity`) — a
  product line within an order; the `UnitPrice` is a **snapshot captured at order time** so a
  later catalog price change does not rewrite history. No identity outside the order.
- **Computed:** line total = `UnitPrice * Quantity`; `TotalAmount` is the persisted sum.

**Statuses:** `Pending → Processing → Paid → Delivered`, with `Cancelled` reachable from the
pre-delivery states.

**Invariants enforced inside the aggregate:**
- An order must contain at least one item.
- Item `Quantity > 0`, `UnitPrice >= 0`.
- A `Delivered` order cannot be cancelled; a `Paid`/`Delivered` order cannot be re-paid.

**Behaviour (the only ways to change state):** `Create`, `MarkProcessing`, `MarkPaid`,
`MarkDelivered`, `Cancel`.

`Order` is the **only** entity loaded/saved as a unit and the consistency boundary for a
transaction.

---

## Cart aggregate (Ordering)

- **Root:** `Cart` (`CartId`, `UserId`) — one active cart per user (unique index on `UserId`).
- **Owned entity:** `CartItem` (`CartItemId`, `ProductId`, `Quantity`).
- **Behaviour:** `AddItem`, `UpdateItemQuantity`, `RemoveItem`, `Clear`. Checkout reads the
  cart, resolves prices from Catalog, creates the `Order`, and clears the cart.

---

## Async flows (event-driven)

The aggregate records domain events; after the order is persisted, a **domain-event dispatcher**
translates them into integration events on Service Bus, and other contexts/the Worker react.

> **Domain-event reliability:** `OrderCreatedEvent` is dispatched to an application handler
> which publishes `OrderCreatedMessage`. A transactional outbox + consumer idempotency remain
> documented future work for full at-least-once safety.

```
1. Place order
   POST /api/v1/orders → checkout cart → Order.Create(...) → saved
       → [OrderCreatedEvent] recorded on the aggregate
       → dispatcher → OrderCreatedMessage on Service Bus (order-events)

2. Process  (Worker reacts to OrderCreatedMessage)
   Worker: load order → MarkPaid()  (placeholder for Payment/Inventory)

3. Confirm & notify  (planned — Notifications reacts to PaymentSucceededEvent)

4. Cancellation flow (planned): payment failure / stock shortage → Order.Cancel()
   → release reservation → notify customer
```

---

## Microsoft Entra ID authentication

Authentication is handled end-to-end with Microsoft Entra (Azure AD). There are no username /
password flows and no API keys — every request is identified by a signed JWT issued by Entra.

### App Registration

| Field | Value |
|---|---|
| **Client ID** | `f58ac8a6-b34d-43f1-9593-3935cb98281d` |
| **Supported account types** | Any Entra ID tenant + personal Microsoft accounts (`AzureADandPersonalMicrosoftAccount`) |
| **Redirect URIs (SPA)** | `http://localhost:4200` (dev), `https://app-quickcart-dev-wl2vpuykc6hy6.azurewebsites.net` (prod) |
| **API scope exposed** | `api://f58ac8a6-b34d-43f1-9593-3935cb98281d/access_as_user` |

### Backend — `Microsoft.Identity.Web`

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(azureAd);          // reads AzureAd:* from config

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireScope("access_as_user")
        .Build();
});
```

**Runtime config (App Service app settings):**

| Key | Value | Why |
|---|---|---|
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Base authority URL |
| `AzureAd__TenantId` | `common` | Accepts any Entra tenant **and** personal Microsoft accounts. Using a specific tenant GUID locks the API to that one tenant; `common` activates `AadIssuerValidator` which validates dynamically from the token's `tid` claim. |
| `AzureAd__ClientId` | `f58ac8a6-...` | Audience validation — tokens must be issued for this app |
| `AzureAd__Scopes` | `access_as_user` | Minimum required scope on every endpoint |

**Multi-tenant issuer validation:** With `TenantId = "common"`, `Microsoft.Identity.Web` registers `AadIssuerValidator` instead of a fixed `ValidIssuer` string. The validator substitutes the `tid` claim from each incoming token into the template `https://login.microsoftonline.com/{tid}/v2.0` and confirms it matches the token's `iss`. Personal Microsoft account tokens (`tid = 9188040d-...`) are accepted alongside organizational accounts.

**Health endpoints** (`/health`, `/health/ready`) are explicitly `.AllowAnonymous()` — probes have no token and must not be blocked by the `FallbackPolicy`.

**Local dev:** `AzureAd__ClientId` is empty in `appsettings.Development.json`, so `entraEnabled = false` and all auth is bypassed. No token is needed to run the API locally.

### Frontend — MSAL Browser 5.x

```typescript
// auth.config.ts
new PublicClientApplication({
  auth: {
    clientId:              environment.auth.clientId,
    authority:            `https://login.microsoftonline.com/${environment.auth.tenantId}`,
    redirectUri:           environment.auth.redirectUri,
    postLogoutRedirectUri: environment.auth.postLogoutRedirectUri,
  },
  cache: { cacheLocation: 'sessionStorage' },
});

// loginRequest — scopes sent at interactive sign-in
{ scopes: ['api://.../access_as_user', 'openid', 'profile', 'email'] }

// apiTokenRequest — scopes used by the auth interceptor per-request
{ scopes: ['api://.../access_as_user'] }
```

**Auth guard** (`authGuard`) — every route is protected; unauthenticated users are redirected
to `loginRedirect()` before any data is fetched.

**Auth interceptor** — attaches `Authorization: Bearer <token>` to all calls whose URL starts
with `environment.apiBaseUrl`. Calls `acquireTokenSilent` first; falls back to
`acquireTokenRedirect` on `InteractionRequiredAuthError`.

**Environment files:**

| File | `tenantId` | `apiBaseUrl` | `redirectUri` |
|---|---|---|---|
| `environment.ts` (dev, `ng serve`) | `common` | `http://localhost:5106/api/v1` | `http://localhost:4200` |
| `environment.prod.ts` (production build) | `common` | `/api/v1` (root-relative) | `https://app-quickcart-dev-wl2vpuykc6hy6.azurewebsites.net` |

The production `apiBaseUrl` is root-relative because the Angular SPA is served from the same
App Service as the API — no CORS headers are needed.

**`fileReplacements` in `angular.json`** ensure the production build substitutes
`environment.prod.ts` for `environment.ts` automatically.

---

## Application features

### API endpoints (v1)

| Endpoint | Description |
|---|---|
| `GET  /api/v1/categories` | List all categories |
| `GET  /api/v1/products` | Paginated products; `?search=` free-text, `?categoryId=` filter |
| `GET  /api/v1/products/{id}` | Single product by GUID |
| `GET  /api/v1/cart` | Current user's cart |
| `PATCH /api/v1/cart` | Add / update / remove cart items |
| `POST  /api/v1/orders` | Checkout — creates order from current cart |
| `GET  /api/v1/orders` | Paginated order history for the signed-in user |
| `GET  /api/v1/orders/{id}` | Single order with line items |
| `GET  /api/v1/users/me` | Current user profile (sync from Entra on first call) |
| `GET  /health` | Liveness — process alive, no dependency checks |
| `GET  /health/ready` | Readiness — SQL + Service Bus reachable |

All non-health endpoints are protected by the `FallbackPolicy` (JWT + `access_as_user` scope).
API versioning via URL segment (`v{version:apiVersion}`); default version is 1.0.

### Frontend pages (Angular 22)

| Route | Component | Description |
|---|---|---|
| `/` | `Home` | Blinkit/Zepto-style home: location picker, category strip, per-category product sections |
| `/products` | `Products` | Flat paginated grid, search bar, category filter |
| `/products/:id` | `ProductDetail` | Single product — description, price, stock, add-to-cart |
| `/cart` | `Cart` | Cart items, quantities, line totals, checkout button |
| `/orders` | `Orders` | Order history list |
| `/orders/:id` | `OrderDetail` | Line items, status, total |

All routes are protected by `authGuard`. State is managed with Angular signals
(`CartState`, `SearchState`).

---

## Security

### Managed Identity — zero secrets

Every Azure resource integration uses Managed Identity; there are no passwords, SAS keys, or
client secrets in any configuration file or environment variable:

| Resource | Auth mechanism |
|---|---|
| Azure SQL Server | `Authentication=Active Directory Default` in connection string; MI acquires an Entra token |
| Service Bus | `DefaultAzureCredential` with `ServiceBusDataSender` + `ServiceBusDataReceiver` RBAC roles |
| Key Vault | `@Microsoft.KeyVault(SecretUri=...)` App Service reference; `Key Vault Secrets User` RBAC role |
| Application Insights | Connection string only (not a secret; no auth required) |

### Request hardening

- **Kestrel request size limit:** 1 MB global cap; `/orders` endpoint tightened to 16 KB via
  `[RequestSizeLimit]`.
- **HTTPS only** (`httpsOnly: true` on App Service; `UseHttpsRedirection` in middleware).
- **HSTS** in production (`UseHsts`).
- **No server banner** — `AddServerHeader = false` on Kestrel.

### Security response headers

Applied by middleware to every response (split by path from Day 32 onward):

**API paths (`/api/*`):**
```
Content-Security-Policy: default-src 'none'; frame-ancestors 'none'
Cache-Control: no-store
```

**SPA paths (everything else):**
```
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline';
                         style-src 'self' 'unsafe-inline'; img-src 'self' data:;
                         connect-src 'self' https://login.microsoftonline.com;
                         frame-ancestors 'none'
```

`script-src 'unsafe-inline'` is required for Angular's beasties deferred-CSS loader
(`<link ... media="print" onload="this.media='all'">`).

All paths also receive:
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Permissions-Policy: geolocation=(), camera=(), microphone=()
```

### OpenAPI

Swagger UI and the OpenAPI document (`/openapi/v1.json`) are served only in `Development`
mode. A `BearerSecuritySchemeTransformer` documents the Entra bearer scheme in the spec so
tooling knows what credential the API expects.

---

## Observability

### OpenTelemetry → Azure Application Insights

```csharp
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("quickcart-api"))
    .UseAzureMonitor();   // reads APPLICATIONINSIGHTS_CONNECTION_STRING
```

Enabled conditionally — only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set (absent in
local dev to avoid telemetry noise). The Worker uses the same pattern with service name
`quickcart-worker`.

**Distributed tracing end-to-end:** Service Bus SDK experimental activity source is enabled
at startup (`Azure.Experimental.EnableActivitySource = true`). This links the API's
order-creation span to the Worker's message-processing span in Application Insights as a
single distributed trace.

### Health checks

```
GET /health        → liveness:  HTTP 200 if the process is alive. No dependency checks.
GET /health/ready  → readiness: HTTP 200 only if SQL Server and Service Bus are reachable.
```

Both endpoints call `.AllowAnonymous()`. Infrastructure health checks are registered from the
Infrastructure layer (not `Program.cs`) via `AddInfrastructureHealthChecks`.

---

## Azure infrastructure (IaC — Bicep + azd)

All resources are defined in Bicep modules under `infra/` and deployed with `azd provision`.

| Resource | Dev SKU | Naming pattern |
|---|---|---|
| App Service Plan (Linux) | B1 | `plan-quickcart-{env}` |
| App Service — API | B1 (shared plan) | `app-quickcart-{env}-{suffix}` |
| App Service — Worker | B1 (shared plan) | `wrk-quickcart-{env}-{suffix}` |
| Azure SQL Server | — | `sql-quickcart-{env}-{suffix}` |
| Azure SQL Database | Basic | `quickcart` |
| Service Bus Namespace | Standard | `sb-quickcart-{env}-{suffix}` |
| Service Bus Queue | — | `order-events` |
| Key Vault (RBAC) | — | `kv-{env}-{suffix}` |
| Application Insights (workspace-based) | — | `appi-quickcart-{env}-{suffix}` |
| Log Analytics Workspace | — | `log-quickcart-{env}-{suffix}` |
| Virtual Network | — | `vnet-quickcart-{env}` |
| Private Endpoints | SQL + Key Vault | `pe-sql-*`, `pe-kv-*` |

**VNet integration:** The App Service and Worker are integrated into the VNet subnet
(`10.10.1.0/24`). All outbound traffic routes through the VNet (`vnetRouteAllEnabled: true`),
so SQL and Key Vault resolve via private DNS zones and their private endpoints.

**`disablePublicNetworkAccess`** defaults to `false` — this allows EF Core migrations and
initial seeding to reach SQL over the public endpoint during first deploy. Flip to `true` once
the private endpoint is validated.

**`azure.yaml`** declares the two services so `azd deploy` publishes and deploys them:
```yaml
services:
  api:    { project: ./src/QuickCart.Api,    language: dotnet, host: appservice }
  worker: { project: ./src/QuickCart.Worker, language: dotnet, host: appservice }
```

**SPA hosting (Day 32):** The Angular production build is copied into
`src/QuickCart.Api/wwwroot/` before `azd deploy`. `Program.cs` uses `UseDefaultFiles` +
`UseStaticFiles` to serve the SPA, and `MapFallbackToFile("index.html")` handles client-side
routing so deep-links (`/cart`, `/orders/:id`) return `index.html` rather than 404.

**EF Core migrations** are applied separately from the running application — `MigrateAsync`
is not called on startup. The schema is managed by running the migration SQL (generated by
`dotnet ef migrations script`) against the database as an Entra admin with `db_ddladmin`
rights. The runtime Managed Identity needs only `db_datareader` + `db_datawriter`.

---

## Testing

| Project | Framework | Scope | Count |
|---|---|---|---|
| `QuickCart.Tests` | xUnit + Moq + FluentAssertions | Domain aggregates, application services | 27 unit tests |
| `QuickCart.IntegrationTests` | WebApplicationFactory + SQLite | API endpoints end-to-end (HTTP → DB) | 15 integration tests |
| `QuickCart.E2E` | Playwright | Full browser flows against a running API | Smoke suite |

**CI pipeline** (`.github/workflows/ci.yml`) runs on push to `main` and `Day-*` branches:
builds in Release, runs unit tests, runs integration tests with coverage, and runs E2E tests
against a locally-started API instance.

---

## Solution layout

```
QuickCart.slnx
├─ src/
│  ├─ QuickCart.Domain          # Bounded-context ownership — no dependencies
│  │   ├─ Catalog               # Category, Product
│  │   ├─ Ordering              # Order (+ OrderItem), Cart (+ CartItem), events, enums
│  │   └─ Shared                # BaseEntity, IDomainEvent, User
│  ├─ QuickCart.Application     # Use cases + ports        → Domain
│  ├─ QuickCart.Contracts       # HTTP request/response DTOs + integration messages
│  ├─ QuickCart.Infrastructure  # EF Core, Service Bus, health checks, DI → Application, Domain
│  ├─ QuickCart.Api             # Controllers, Program.cs, SPA hosting → Application, Contracts, Infra
│  └─ QuickCart.Worker          # Hosted service: Service Bus consumer → Infrastructure
├─ web/                         # Angular 22 SPA
│  ├─ src/app/
│  │   ├─ core/                 # auth (MSAL), api services, state signals
│  │   └─ features/             # home, products, product-detail, cart, orders, order-detail
│  ├─ src/environments/         # environment.ts (dev) + environment.prod.ts (production)
│  └─ public/                   # Static assets (SVG product images, favicon)
├─ infra/                       # Bicep modules + main.bicep + bicepparam files
│  └─ modules/                  # appservice, appservice-worker, sql, servicebus, keyvault,
│                               # monitoring, network, rbac, storageaccount
├─ tests/
│  ├─ QuickCart.Tests           # xUnit unit tests — domain aggregates + application services
│  ├─ QuickCart.IntegrationTests # WebApplicationFactory + SQLite integration tests
│  └─ QuickCart.E2E             # Playwright end-to-end tests
└─ azure.yaml                   # azd service definitions (api + worker)
```

**Dependency rule:** Domain depends on nothing. Application depends only on Domain.
Infrastructure and Api depend inward. `Contracts` is the public API/wire shape, kept separate
from domain types. `Worker` depends on Infrastructure and Application.

**Persistence:** EF Core 10. SQL Server in the cloud via Managed Identity
(`Authentication=Active Directory Default`). SQLite in local dev and integration tests (no
Azure credentials required). One migration (`InitialCreate`) describes the full schema — 7
application tables + the EF migrations history table.
