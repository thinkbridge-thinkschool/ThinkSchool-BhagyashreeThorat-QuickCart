# Day 26 — App Insights + KQL

Make production legible: OpenTelemetry → Application Insights, then KQL for latency,
dependency breakdown, and error rate, plus an alert and an end-to-end distributed trace
spanning **API → Service Bus → Worker → SQL**.

## What was wired

| Piece | Where |
|---|---|
| `OrderCreatedMessage` (wire contract) | `src/QuickCart.Contracts/Messaging/OrderCreatedMessage.cs` |
| API publishes to `order-events` | `OrderService` → `ServiceBusOrderEventPublisher` (Infrastructure) |
| Worker consumes + writes SQL | `src/QuickCart.Worker/OrderEventsConsumer.cs` (marks order Paid) |
| OTel in API | `Program.cs` → `AddOpenTelemetry().UseAzureMonitor()` (role `quickcart-api`) |
| OTel in Worker | `Program.cs` → `UseAzureMonitor()` (role `quickcart-worker`) |
| App Insights + Log Analytics | `infra/modules/monitoring.bicep` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | set on both apps via Bicep |

The Azure SDK stamps the W3C `traceparent` onto each Service Bus message, and the Worker's
OTel restores it — that single fact is what stitches the two processes into one trace.

### Two gotchas that actually mattered

1. **Azure SDK messaging tracing is opt-in.** Service Bus send/process spans are gated behind
   an experimental switch. Without it there is no producer dependency and no consumer span, so
   the Worker's SQL calls show up as orphaned root operations. Both hosts set, before any client:
   ```csharp
   AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
   ```
2. **`cloud_RoleName` is the App Service site name**, not the OTel `AddService` name — the
   distro picks it up from App Service. So in queries the roles are
   `app-quickcart-dev-…` (API) and `wrk-quickcart-dev-…` (Worker). The queries below match on
   the `app-` / `wrk-` prefixes so they keep working regardless of the unique suffix.

---

## Run the queries

App Insights → **Logs** (or Log Analytics workspace → Logs). Default time range covers the
last traffic you generated; each query also pins its own `ago()` window.

### 1. p50 / p95 / p99 latency by endpoint

```kql
requests
| where timestamp > ago(1h)
| summarize
    requests = count(),
    p50_ms = round(percentile(duration, 50), 1),
    p95_ms = round(percentile(duration, 95), 1),
    p99_ms = round(percentile(duration, 99), 1)
  by operation_Name, cloud_RoleName
| order by p99_ms desc
```

`duration` is in milliseconds. `cloud_RoleName` separates `quickcart-api` from `quickcart-worker`.

### 2. Dependency call breakdown (time in SQL, Service Bus, …)

```kql
dependencies
| where timestamp > ago(1h)
| summarize
    calls    = count(),
    avg_ms   = round(avg(duration), 1),
    p99_ms   = round(percentile(duration, 99), 1),
    total_ms = round(sum(duration), 0),
    failures = countif(success == false)
  by type, target, cloud_RoleName
| order by total_ms desc
```

Expect `type` values like **SQL** (EF Core / `Microsoft.Data.SqlClient`) and
**Azure Service Bus** / **Queue Message** for the publish + consume hops.

### 3. Error rate

Overall, single number:

```kql
requests
| where timestamp > ago(1h)
| summarize total = count(), failed = countif(success == false)
| extend error_rate_pct = round(100.0 * failed / total, 2)
```

Trended over time, per role (good for the alert preview and a chart):

```kql
requests
| where timestamp > ago(1h)
| summarize total = count(), failed = countif(success == false)
    by bin(timestamp, 5m), cloud_RoleName
| extend error_rate_pct = round(100.0 * failed / total, 2)
| order by timestamp asc
| render timechart
```

---

## Verify the distributed trace (API → SB → Worker → SQL)

### Find a trace that spans both roles

```kql
union requests, dependencies
| where timestamp > ago(1h)
| extend role = case(cloud_RoleName startswith "app-", "api",
                     cloud_RoleName startswith "wrk-", "worker", "other")
| summarize roles = make_set(role) by operation_Id
| where set_has_element(roles, "api") and set_has_element(roles, "worker")
| take 10
```

Copy one `operation_Id`, then expand the full end-to-end timeline:

```kql
union requests, dependencies, traces, exceptions
| where operation_Id == "<paste-operation_Id-here>"
| project timestamp, itemType, cloud_RoleName,
          name = coalesce(name, message), type, target,
          duration, success
| order by timestamp asc
```

You should see, in order:
1. `quickcart-api` request `POST Orders`
2. `quickcart-api` dependency **SQL** (insert order)
3. `quickcart-api` dependency **Azure Service Bus** (send)
4. `quickcart-worker` request/consumer span (process message)
5. `quickcart-worker` dependency **SQL** (update order → Paid)

### Screenshot (the deliverable)

Portal → Application Insights → **Transaction search** → open a `POST Orders` request →
**View timeline** (the end-to-end transaction / Gantt view). It shows the API and Worker
spans nested under one operation. Screenshot that. The **Application Map** (API → Service
Bus → Worker → SQL Database nodes) is a good second screenshot.

---

## Error-rate alert

Log-query (scheduled query) alert: fire when the rolling error rate exceeds 10%.
Replace `<rg>` and `<appInsightsResourceId>` (from `azd env get-values` /
`az monitor app-insights component show`).

```bash
# 1. Action group (who gets paged) — once.
az monitor action-group create \
  --name quickcart-oncall \
  --resource-group <rg> \
  --short-name qconcall \
  --action email primary you@example.com

# 2. The alert rule. The query returns a row only when the error rate is > 10%,
#    so "number of rows > 0" is the fire condition.
az monitor scheduled-query create \
  --name "QuickCart-HighErrorRate" \
  --resource-group <rg> \
  --scopes "<appInsightsResourceId>" \
  --description "API/Worker error rate above 10% over the last 15 minutes" \
  --severity 2 \
  --evaluation-frequency 5m \
  --window-size 15m \
  --condition "count 'unhealthy' > 0" \
  --condition-query unhealthy='requests | summarize total = count(), failed = countif(success == false) | where total > 0 | extend rate = 100.0 * failed / total | where rate > 10' \
  --action-groups <actionGroupResourceId>
```

Portal equivalent: App Insights → **Alerts → Create → Alert rule** → *Custom log search*,
paste the inner KQL, threshold *Number of results > 0*, eval every 5 min over 15 min.

Deployed instance: rule **QuickCart-HighErrorRate** (sev 2) → action group **quickcart-oncall**
(email). The synthetic 404/400 traffic pushed the error rate to ~24%, so the rule fired —
proof the path works end to end.

---

## Verified run (what the deliverable looks like)

Resource: App Insights `appi-quickcart-dev-wl2vpuykc6hy6` in `rg-quickcart-dev`.

One real end-to-end trace, `operation_Id = f7f5c70bbc44e4fd5e9b2382ff13ab12`, expanded with
the timeline query above (durations inflated by post-deploy cold-start MI token acquisition):

| seq | role | item | name |
|----|------|------|------|
| 1 | API | request | `POST api/orders` |
| 2 | API | dependency | `DefaultAzureCredential.GetToken` / `GET /msi/token` (MI) |
| 3 | API | dependency **SQL** | `SQL: quickcart` (insert order) |
| 4 | API | dependency **servicebus** | `ServiceBusSender.Send` (publish) |
| 5 | WORKER | request | `ServiceBusProcessor.ProcessMessage` (consumer, same trace) |
| 6 | WORKER | dependency **SQL** | `SQL: quickcart` (load order) |
| 7 | WORKER | dependency **SQL** | `SQL: quickcart` (update → Paid) |
| 8 | WORKER | dependency | `ServiceBusReceiver.Complete` |

15 of 15 orders produced a trace spanning both `app-` and `wrk-` roles. SQL confirms the
Worker did its job: all orders end in `Status = Paid (1)`.

### Screenshot to submit

Portal → Application Insights `appi-quickcart-dev-wl2vpuykc6hy6` → **Transaction search** →
open a `POST api/orders` transaction (or **Investigate → Application map**). The end-to-end
timeline shows the API and Worker spans under one operation. Direct link to the resource:

```
https://portal.azure.com/#@/resource/subscriptions/ab101448-0c54-42b1-bbe2-3e807e0ad1c2/resourceGroups/rg-quickcart-dev/providers/Microsoft.Insights/components/appi-quickcart-dev-wl2vpuykc6hy6/overview
```
