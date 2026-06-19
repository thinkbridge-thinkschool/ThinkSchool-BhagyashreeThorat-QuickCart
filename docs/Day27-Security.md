# Day 27 — Security Pass

Threat-model the QuickCart capstone (STRIDE-lite), put the data tier behind private
endpoints, harden the OpenAPI surface (auth, versioning, input limits), and run a basic
OWASP ZAP baseline. Everything below reflects the **actual** codebase — no invented services.

---

## 0. Project assessment (baseline before this pass)

- **Architecture:** .NET 10 modular monolith, clean architecture. `QuickCart.Api` (controllers)
  + `QuickCart.Worker` (Service Bus consumer) over shared Domain/Application/Infrastructure/Contracts.
  Deployed by `azd`/Bicep to Azure App Service (Linux B1) with Azure SQL, Service Bus (Standard),
  Key Vault, App Insights + Log Analytics.
- **Auth:** Entra ID via `Microsoft.Identity.Web` JWT bearer, enabled only when `AzureAd:ClientId`
  is configured (cloud); a `FallbackPolicy` requires an authenticated principal on every endpoint.
  Local dev runs open (no app registration).
- **Already secure:** Managed Identity everywhere (no connection-string secrets), Key Vault
  references + RBAC, `httpsOnly`, `minTlsVersion 1.2` (App Service/SQL/Service Bus),
  `ftpsState Disabled`, EF Core parameterized queries, domain-layer invariants
  (`Order.Create`, `OrderLine` ctor).

### Gap analysis (only real gaps)

| Requirement | Current State (before) | Change made |
|---|---|---|
| STRIDE threat model | none | this document (§2) |
| Data tier private endpoints | SQL / Key Vault / Service Bus all `publicNetworkAccess: Enabled`; SQL allowed all Azure IPs (0.0.0.0) | VNet + private endpoints + DNS for **SQL & Key Vault**; App Service/Worker VNet integration; public access flag to disable after validation (§3). Service Bus excluded — see §3.4 |
| OpenAPI auth surfaced | enforced but not described in the document | bearer security scheme via document transformer (§4.1) |
| API versioning | none | `Asp.Versioning` → `/api/v1/orders` (§4.2) |
| Request/input limits | Kestrel defaults only (30 MB, unbounded `Lines[]`) | 1 MB global cap + 64 KB on POST + DTO `MaxLength`/`Range`/`StringLength` (§4.3) |
| Input validation at the boundary | domain-only (exception → 400) | DataAnnotations on DTOs → automatic 400 `ValidationProblemDetails` (§4.3) |
| Security headers | none | CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy (§4.4) |
| Error leakage | raw `ex.Message` / default stack traces | `ProblemDetails` + `UseExceptionHandler`; `Server` header suppressed (§4.4) |

---

## 1. Threat model — STRIDE-lite

Scope: the Ordering slice that exists today (`POST /api/v1/orders`, `GET /api/v1/orders/{id}`),
its SQL/Service Bus/Key Vault dependencies, and the Worker.

| STRIDE | Threat / attack scenario | Impact | Existing controls | Additional mitigation (this pass) |
|---|---|---|---|---|
| **Spoofing** | Caller posts an order with no/forged token, impersonating a customer. | Fraudulent orders | Entra JWT + `FallbackPolicy` (cloud); TLS 1.2 | Bearer scheme now described in OpenAPI; **(future)** require a scope/role policy, not just "authenticated" |
| **Tampering** | Negative `UnitPrice`/`Quantity`, 10k-line array, or `CustomerId` swap in the body; MITM. | Price/total manipulation, integrity loss | Domain invariants; `httpsOnly`; EF parameterized queries (no SQLi) | DTO `Range`/`StringLength`/`MaxLength` reject at the edge; `Lines` capped at 100; **(future)** derive `CustomerId` from the token claim |
| **Repudiation** | User denies placing an order; no per-request principal recorded. | Disputes, weak audit | App Insights request/dependency telemetry + trace correlation | **(future)** log authenticated `sub`/`oid` with the order id |
| **Information Disclosure** | Data tier reachable from the public internet; raw exception text returned. | DB/secret exposure; internal leakage | MI / no secrets; KV RBAC; TLS 1.2 | **Private endpoints + public access disabled** for SQL & Key Vault; `ProblemDetails` (no stack traces); `Server` header off; CSP |
| **Denial of Service** | Oversized body or huge `Lines[]` exhausts memory/CPU. | Outage, cost | App Service `alwaysOn`; Kestrel 30 MB default | 1 MB global + 64 KB POST request-size cap; `Lines` count cap (100); **(future)** rate limiting / WAF |
| **Elevation of Privilege** | Any authenticated user acts with full rights; broad SQL firewall (0.0.0.0). | Lateral movement | MI least-privilege RBAC; contained SQL users | 0.0.0.0 firewall rule removed in PE mode; **(future)** scope-based authorization policy |

---

## 2. Private endpoint change

**File:** `infra/modules/network.bicep` (new) + wiring in `infra/main.bicep`, `infra/modules/sql.bicep`,
`infra/modules/keyvault.bicep`, `infra/modules/appservice.bicep`, `infra/modules/appservice-worker.bicep`.

### 2.1 What was added

- A VNet (`10.10.0.0/16`) with two subnets:
  - `snet-app` (`10.10.1.0/24`) delegated to `Microsoft.Web/serverFarms` for App Service regional
    VNet integration.
  - `snet-pe` (`10.10.2.0/24`) with `privateEndpointNetworkPolicies: Disabled` for the PE NICs.
- Private DNS zones `privatelink<sqlServerHostname>` and `privatelink.vaultcore.azure.net`, each
  linked to the VNet, so the public FQDNs resolve to the private IPs from inside the VNet.
- Private endpoints for the **SQL server** (`groupIds: ['sqlServer']`) and **Key Vault**
  (`groupIds: ['vault']`), each with a `privateDnsZoneGroups/default` entry.
- App Service **and** Worker now set `virtualNetworkSubnetId` + `vnetRouteAllEnabled` so their
  outbound SQL/Key Vault calls travel the VNet and resolve the private IPs.

### 2.2 Disabling public access (the controlled cut-over)

`sql.bicep` and `keyvault.bicep` gained a `publicNetworkAccess` parameter, driven from
`main.bicep`'s `disablePublicNetworkAccess` flag:

```bicep
publicNetworkAccess: disablePublicNetworkAccess ? 'Disabled' : 'Enabled'
```

The SQL `AllowAllAzureIps` (0.0.0.0) firewall rule is now conditional and is **dropped** the moment
public access is disabled:

```bicep
resource allowAzureServices '...firewallRules@...' = if (publicNetworkAccess == 'Enabled') { ... }
```

**Cut-over procedure (documented, not auto-applied):**
1. First deploy with `disablePublicNetworkAccess = false` (default) so EF migrations / seed from
   the dev box can still reach SQL, and the private endpoints come up.
2. Validate the app reaches SQL/Key Vault **through the VNet** (App Service → integrated subnet →
   PE → private DNS).
3. Redeploy with `disablePublicNetworkAccess = true`. SQL & Key Vault then reject public traffic;
   only the VNet path remains. After this, run migrations from inside the VNet (a jumpbox/Bastion,
   self-hosted agent, or a temporary scoped firewall rule), **not** the open laptop.

### 2.3 Toggle

`enablePrivateNetworking` (default `true`) deploys the whole networking module; set it `false` to
fall back to the public-endpoint topology (e.g. a throwaway test RG).

### 2.4 Why Service Bus is excluded (not pretended)

Service Bus **Private Link requires the Premium tier.** The dev environment runs **Standard**
(`infra/main.dev.bicepparam`), so adding a Service Bus private endpoint would force a ~10x cost
increase. It therefore stays on its public endpoint — still protected by **Managed Identity + RBAC
with no SAS keys** and TLS 1.2. The prod params (`main.prod.bicepparam`) already select
`serviceBusSku = 'Premium'`, so prod can add a Service Bus PE later with **no application change**.

---

## 3. OpenAPI hardening changes

### 3.1 Bearer auth scheme in the document
**Files:** `src/QuickCart.Api/OpenApi/BearerSecuritySchemeTransformer.cs` (new), `Program.cs`.
An `IOpenApiDocumentTransformer` adds an HTTP `bearer`/`JWT` security scheme to
`components.securitySchemes` and a document-level security requirement, so generated clients and
reviewers can see the API requires an Entra JWT. (Enforcement still comes from the `FallbackPolicy`.)

### 3.2 API versioning
**Files:** `QuickCart.Api.csproj` (`Asp.Versioning.Mvc`), `Program.cs`, `OrdersController.cs`.
`AddApiVersioning` (default `1.0`, `ReportApiVersions = true`) + route
`api/v{version:apiVersion}/orders` → live route is now **`/api/v1/orders`**. Responses carry
`api-supported-versions: 1.0`. Applies to every controller, including the planned
Catalog/Inventory/etc. contexts.

### 3.3 Request size + input limits
**Files:** `Program.cs`, `OrdersController.cs`, `CreateOrderRequest.cs`.
- Kestrel `MaxRequestBodySize = 1 MB` (global backstop); `[RequestSizeLimit(64 * 1024)]` on the
  order POST.
- DTO DataAnnotations (on the record **constructor parameters**, as .NET 10 requires):
  `Lines` `MinLength(1)`/`MaxLength(100)`; `ProductName` `StringLength(200, min 1)`;
  `UnitPrice` `Range(0, 1_000_000)`; `Quantity` `Range(1, 10_000)`. `[ApiController]` turns failures
  into automatic `400 ValidationProblemDetails` before the domain runs.

### 3.4 Security headers, ProblemDetails, server-header suppression
**File:** `Program.cs`.
- Middleware sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`,
  `Permissions-Policy: geolocation=(), camera=(), microphone=()` on every response.
- `AddProblemDetails()` + `UseExceptionHandler()` → unhandled errors return RFC 7807 ProblemDetails,
  never a stack trace. The controller's domain-exception catch now returns `Problem(...)` too.
- Kestrel `AddServerHeader = false` removes the `Server` banner.
- `UseHsts()` in non-Development.

---

## 4. OWASP ZAP baseline

The API is a JSON service (no UI), so the baseline scan effectively checks transport/response-header
hygiene and information disclosure. Run it against a running instance:

```bash
# Start the API locally (Development, in-memory store, auth open):
dotnet run --project src/QuickCart.Api      # listens on http://localhost:5106

# Baseline scan via the official ZAP container (passive only, no attack):
docker run --rm -t ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t http://host.docker.internal:5106 -r zap-report.html
```

For a deployed target, point `-t` at `https://app-quickcart-<env>-<suffix>.azurewebsites.net`.

### Findings (typical ZAP baseline alerts for an unhardened ASP.NET Core API) and fixes

| ZAP alert | Severity | Before | Fix applied | Expected re-scan |
|---|---|---|---|---|
| Content Security Policy (CSP) Header Not Set | Medium | missing | CSP `default-src 'none'` added | PASS |
| Missing Anti-clickjacking Header | Medium | missing | `X-Frame-Options: DENY` (+ CSP `frame-ancestors 'none'`) | PASS |
| X-Content-Type-Options Header Missing | Low | missing | `X-Content-Type-Options: nosniff` | PASS |
| Server Leaks Version Information (`Server` header) | Low | `Server: Kestrel` | `AddServerHeader = false` | PASS |
| Strict-Transport-Security Header Not Set | Low | missing | `UseHsts()` (non-Dev, over HTTPS) | PASS on HTTPS target |
| Information disclosure via error stack trace | Low/Info | raw messages possible | `ProblemDetails` + `UseExceptionHandler` | PASS |
| Permissions-Policy Header Not Set | Info | missing | `Permissions-Policy` added | PASS |

**Verified locally** (running instance, `GET /api/v1/orders/{id}`) — headers now present, `Server`
header absent:

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Content-Security-Policy: default-src 'none'; frame-ancestors 'none'
Permissions-Policy: geolocation=(), camera=(), microphone=()
api-supported-versions: 1.0
```

Not applicable: **Anti-CSRF tokens** (no cookie auth/forms — stateless bearer-token JSON API);
**SQL injection** (EF Core parameterizes all queries).

### Dependency finding (out of band)
`dotnet build` reports **NU1902**: `OpenTelemetry.Api 1.12.0` (transitive in `QuickCart.Worker`) has a
known moderate-severity advisory (GHSA-g94r-2vxg-569j). **Recommended fix:** bump
`Azure.Monitor.OpenTelemetry.AspNetCore` to a build that pulls a patched `OpenTelemetry.Api`, or add a
direct pinned `PackageReference`. **Not changed in this pass** to avoid a risky transitive upgrade
without a regression cycle — flagged honestly rather than silently bumped.

---

## 5. What was fixed

1. **API versioning** — `/api/orders` → `/api/v1/orders`; old route returns 404 (verified).
2. **Request size limits** — 1 MB global + 64 KB on the order POST.
3. **Input validation at the boundary** — DTO DataAnnotations; malformed input → 400
   `ValidationProblemDetails` (verified: empty `Lines`, negative `Quantity`).
4. **Bug found & fixed while testing** — DataAnnotations were first placed with `[property:]`
   targets; .NET 10's validation throws `InvalidOperationException` for that on record positional
   parameters, turning *every* invalid request into a **500**. Moved the attributes onto the
   constructor parameters; invalid requests now correctly return **400**.
5. **OpenAPI auth** — Entra bearer scheme described in the document.
6. **Security headers + error hygiene** — CSP/X-Content-Type-Options/X-Frame-Options/
   Referrer-Policy/Permissions-Policy; `ProblemDetails`; `Server` header suppressed; HSTS in prod.
7. **Private endpoints** — VNet + PE + private DNS for SQL & Key Vault; App Service/Worker VNet
   integration; flag to disable public access after validation; 0.0.0.0 SQL firewall removed in PE mode.

---

## 6. Verification performed

- `dotnet build QuickCart.slnx` → **0 errors** (2 NU1902 advisory warnings, documented in §4).
- `dotnet test QuickCart.slnx` → **4/4 passed**.
- `az bicep build --file infra/main.bicep` → **EXIT 0, 0 warnings** (BCP318 suppressed with an
  explanatory `#disable-next-line`).
- **Runtime probes against a live instance** (`dotnet run`, port 5106):
  - `GET /api/v1/orders/{guid}` → security headers present, no `Server` header,
    `api-supported-versions: 1.0`.
  - `POST` empty `Lines` → **400** `{ "errors": { "Lines": ["An order must contain at least one line."] } }`.
  - `POST` `quantity: -3` → **400** `{ "errors": { "Lines[0].Quantity": ["Quantity must be between 1 and 10,000."] } }`.
  - `POST` valid order → **201 Created**.
  - Legacy `GET /api/orders/{guid}` (unversioned) → **404**.
