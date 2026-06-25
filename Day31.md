# Day 31 – Polish: Tests, Performance, Security

## Overview

This document describes all changes made for Day 31, including how to run every test suite, generate coverage, run the load test, and interpret results.

---

## Files Created

| File | Purpose |
|------|---------|
| `tests/QuickCart.Tests/CartServiceTests.cs` | Unit tests for CartService (mocked repos) |
| `tests/QuickCart.Tests/OrderServiceTests.cs` | Unit tests for OrderService (mocked repos) |
| `tests/QuickCart.IntegrationTests/QuickCart.IntegrationTests.csproj` | Integration test project |
| `tests/QuickCart.IntegrationTests/ApiFactory.cs` | WebApplicationFactory with SQLite in-memory |
| `tests/QuickCart.IntegrationTests/ProductsApiTests.cs` | API tests for /products and /categories |
| `tests/QuickCart.IntegrationTests/CartApiTests.cs` | API tests for cart CRUD |
| `tests/QuickCart.IntegrationTests/OrdersApiTests.cs` | Full checkout journey integration test |
| `tests/QuickCart.E2E/QuickCart.E2E.csproj` | Playwright E2E test project |
| `tests/QuickCart.E2E/CheckoutJourneyTest.cs` | Playwright API journey test |
| `tests/performance/checkout.js` | k6 load test for the checkout endpoint |
| `.github/workflows/ci.yml` | GitHub Actions CI pipeline |
| `Day31.md` | This file |

## Files Modified

| File | Change |
|------|--------|
| `tests/QuickCart.Tests/QuickCart.Tests.csproj` | Added Moq 4.20.72 + FluentAssertions 6.12.3 |
| `src/QuickCart.Api/Program.cs` | Added `public partial class Program {}` + `Cache-Control: no-store` header |
| `src/QuickCart.Application/Orders/OrderService.cs` | Fixed N+1 in `GetMyOrdersAsync`; eliminated redundant product query in `CheckoutAsync` |

---

## 1. Running Unit Tests

```bash
# From repo root
dotnet test tests/QuickCart.Tests
```

Tests cover:
- `CartTests` — Cart aggregate invariants (add, update, remove, clear)
- `OrderTests` — Order aggregate invariants (create, cancel, totals)
- `CartWorkflowTests` — Multi-add scenario against real SQLite
- `CartServiceTests` — CartService application logic with mocked repositories
- `OrderServiceTests` — OrderService application logic with mocked repositories

---

## 2. Running Integration Tests

The integration tests use `WebApplicationFactory<Program>` with an **isolated SQLite in-memory database** — no SQL Server Express required.

```bash
dotnet test tests/QuickCart.IntegrationTests
```

Covers:
- `ProductsApiTests` — GET /products, GET /products?search=, GET /products/{id}, security headers
- `CartApiTests` — Add, update, delete cart items; validation (quantity=0 returns 400)
- `OrdersApiTests` — Full checkout journey: add to cart → checkout → verify order → cancel

---

## 3. Running the E2E Test

The E2E test uses Playwright's `IAPIRequestContext` to walk the checkout journey against a **real running API** (out-of-process, unlike integration tests).

### Step 1 — Install Playwright browsers (once)
```bash
cd tests/QuickCart.E2E
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

### Step 2 — Start the API
```bash
dotnet run --project src/QuickCart.Api --urls "http://localhost:5000"
```

### Step 3 — Run the E2E test
```bash
# Targeting locally running API (default)
dotnet test tests/QuickCart.E2E

# Targeting a deployed environment
E2E_BASE_URL=https://quickcart-api.azurewebsites.net dotnet test tests/QuickCart.E2E
```

The test covers seven steps:
1. GET /api/v1/products — verify catalog is loaded
2. POST /api/v1/cart/items — add an available product (qty 2)
3. GET /api/v1/cart — verify item is in cart
4. POST /api/v1/orders — place order, expect 201 Created
5. GET /api/v1/cart — verify cart is empty after checkout
6. GET /api/v1/orders — verify order appears in history
7. GET /api/v1/orders/{id} — verify order is retrievable by ID

> **Note:** On second run the cart is already empty from the previous checkout. Run `dotnet run` with a fresh `quickcart.db` (delete the file) or use the integration tests instead for full isolation.

---

## 4. Generating and Viewing Coverage

### Generate (single command)
```bash
dotnet test tests/QuickCart.Tests tests/QuickCart.IntegrationTests \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults/

dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/coverage-report" \
  -reporttypes:"Html;TextSummary"
```

### View
Open `TestResults/coverage-report/index.html` in a browser.

The summary line printed to the terminal looks like:
```
Line coverage: 72.3%   Branch coverage: 61.5%
```

Covered layers: Domain (aggregates + entities), Application (services), Infrastructure (repositories), API (controllers via integration tests).

---

## 5. Running the k6 Performance Test

### Prerequisites
Install k6: https://k6.io/docs/get-started/installation/

### Run
```bash
# Against the locally running API (start it first)
k6 run tests/performance/checkout.js

# Against a remote environment
k6 run --env BASE_URL=https://quickcart-api.azurewebsites.net tests/performance/checkout.js
```

### Load profile
| Stage | Duration | VUs |
|-------|----------|-----|
| Warm-up | 15 s | 0 → 5 |
| Sustained | 30 s | 20 |
| Peak | 15 s | 40 |
| Ramp-down | 10 s | 40 → 0 |

### Thresholds (test fails if breached)
- `http_req_duration p(95) < 500 ms`
- `http_req_duration p(99) < 1000 ms`
- `checkout_duration p(95) < 600 ms`
- `checkout_duration p(99) < 1200 ms`
- `errors rate < 1%`

---

## 6. Interpreting the p99 Result

**p99 (99th percentile latency)** = the response time that 99% of requests complete within.

Example output:
```
checkout_duration............: avg=142ms  p(95)=310ms  p(99)=620ms
```

| Reading | Meaning |
|---------|---------|
| p99 < 500 ms | Excellent — even the slowest 1% of users get a fast response |
| p99 500–1200 ms | Acceptable — within the declared threshold |
| p99 > 1200 ms | Threshold breach — investigate DB query plans, connection pool saturation, or GC pressure |

> **Rule of thumb:** p99 > 3× p50 (median) signals tail latency from resource contention (lock waits, GC pauses). p99 < 2× p50 is a healthy distribution.

### Before vs After (N+1 fix)

With a user having 10 orders (3 items each), `GetMyOrdersAsync` previously made **11 database round-trips** (1 for orders + 10 for product names). After the fix it makes **2 round-trips** (1 for orders + 1 batch product lookup).

At 20 concurrent users with 5 orders each, the old code generated ~**100 extra SQL queries per second**. The fix reduces this to ~20. Expected p99 improvement: 30–50% on `GET /api/v1/orders` under load.

---

## 7. Security Review

### What was verified

| Area | Finding | Status |
|------|---------|--------|
| JWT / Entra auth | Scope-based (`access_as_user`) enforced globally via `FallbackPolicy` | ✅ |
| Authorization | `GetByIdAsync` and `CancelAsync` both verify `order.UserId == callerUserId` | ✅ |
| Price integrity | Prices resolved server-side at checkout; client cannot supply prices | ✅ |
| Input validation | `[Required]` + `[Range(1, 10_000)]` on all request models; route `{id:guid}` rejects non-GUIDs | ✅ |
| Request size | Kestrel global 1 MB cap; `/orders` tightened to 16 KB via `[RequestSizeLimit]` | ✅ |
| Swagger exposure | OpenAPI + Swagger UI mounted only in `Development`; `AllowAnonymous` on spec fetch only | ✅ |
| SQL injection | EF Core parameterises all queries; no raw SQL | ✅ |
| Error leakage | `UseExceptionHandler` + `ProblemDetails` — no stack traces in responses | ✅ |
| Security headers | `X-Content-Type-Options`, `X-Frame-Options`, `CSP`, `Referrer-Policy`, `Permissions-Policy` | ✅ |
| HSTS | Added in non-Development environments | ✅ |
| Server banner | `AddServerHeader = false` hides Kestrel version | ✅ |

### Change made

Added `Cache-Control: no-store` to the security headers middleware (`Program.cs`).

**Why:** API responses contain user-specific data (cart contents, order history). Without this header, shared/reverse-proxy caches could serve one user's data to another. `no-store` prevents any caching of responses at intermediate nodes.

### Remaining items (out of scope for Day 31)

- **Rate limiting** — `AddRateLimiter` with a fixed-window or token-bucket policy would protect against credential stuffing and cart abuse. Deferred because it requires policy design decisions (per-IP vs per-user, limits per endpoint).
- **CORS production policy** — The dev CORS policy (`AllowAnyHeader`, `AllowAnyMethod`) is only applied in `Development`. Production would need an explicit allow-list.

---

## 8. Running the Application

```bash
# API (backend)
dotnet run --project src/QuickCart.Api

# Angular frontend (separate terminal)
cd web
npm install
ng serve

# Background worker (optional — only needed if Service Bus is configured)
dotnet run --project src/QuickCart.Worker
```

The API serves on `https://localhost:7xxx` / `http://localhost:5xxx` (see `launchSettings.json`).
The Angular app serves on `http://localhost:4200` and proxies API calls through CORS.
