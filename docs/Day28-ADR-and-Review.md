# ADR-001 – Build the Ordering Slice as a Modular Monolith with Clean Architecture

## Status

Accepted

## Context

QuickCart is a quick-commerce ordering system. The design includes five business domains: Ordering, Catalog, Inventory, Payment, and Notifications. The Ordering domain is implemented first, while the other domains are planned for future expansion.

At the start of the project, I needed an architecture that would allow me to build and deploy a working ordering workflow quickly while still keeping clear boundaries between business areas and leaving room for future growth.

## Decision

I chose a Modular Monolith with Clean Architecture.

The solution is separated into Domain, Application, Infrastructure, API, and Contracts projects. Business rules are kept in the domain layer, application logic is isolated from infrastructure concerns, and dependencies flow inward.

The Ordering domain acts as the core business boundary. Communication with external systems such as Azure SQL, Service Bus, Application Insights, and Key Vault happens through abstractions so infrastructure can change without affecting business logic.

Asynchronous integration was chosen for background processing, allowing order creation and downstream processing to remain loosely coupled.

## Alternatives Considered

### Microservices

**Pros**

* Independent deployment
* Independent scaling
* Strong service isolation

**Cons**

* More infrastructure to manage
* Higher operational complexity
* More difficult local development and debugging
* Higher Azure cost for a capstone project

### Traditional N-Tier CRUD Application

**Pros**

* Faster initial development
* Simpler structure

**Cons**

* Business rules become scattered across layers
* Tighter coupling between application and database
* Harder to evolve into separate domains later

### Modular Monolith (Chosen)

**Pros**

* Clear architectural boundaries
* Easier development and debugging
* Lower infrastructure cost
* Simpler deployment model
* Easier path to future service extraction

**Cons**

* The solution is developed and managed as a single codebase, but deployment now includes both an API host and a Worker host that must be operated together.
* Modules cannot scale independently.
* Additional abstraction introduces some complexity before multiple business domains exist.

## Why This Decision Was Made

The project needed to balance learning architecture patterns with keeping the implementation practical.

A modular monolith provided domain boundaries, clean dependency flow, infrastructure flexibility, and lower operational overhead while still supporting future expansion into separate services if needed.

## Consequences

### Positive

* Clear separation of concerns.
* Testable domain layer.
* Easier local development.
* Simpler deployment process.
* Infrastructure concerns remain isolated behind abstractions.
* Authentication, observability, security, and messaging could be added without changing core business rules.

### Negative

* The API and Worker now operate as separate running components that must be monitored together.
* Scaling happens at the application level rather than the business-domain level.
* Future growth may require some domains to be extracted into separate services.

---

# Day-by-Day Build Plan

## Day 22 – Capstone Kickoff: Design + Scaffold

* Designed the Ordering bounded context and identified future contexts (Catalog, Inventory, Payment, Notifications).
* Defined the Order aggregate as the core business entity.
* Documented asynchronous processing flows.
* Created the Clean Architecture solution structure.
* Scaffolded Domain, Application, Infrastructure, API, and Contracts projects.
* Added initial unit tests.

**Outcome:** Working project structure with clear architectural boundaries and a documented design.

---

## Day 23 – Bicep Infrastructure as Code

* Created reusable Bicep modules for App Service, Azure SQL, Service Bus, Key Vault, Monitoring, and RBAC.
* Added separate parameter files for development and production environments.
* Configured infrastructure security defaults such as HTTPS-only access and minimum TLS versions.
* Validated infrastructure definitions before deployment.

**Outcome:** Entire cloud infrastructure became repeatable, version-controlled, and deployable from source.

---

## Day 24 – Deployment Stacks + azd

* Configured Azure Developer CLI.
* Added Azure Deployment Stacks support.
* Created deployment workflows for development and production environments.
* Verified deployment execution for both environments.
* Established an environment promotion workflow.

**Outcome:** Deployments became consistent, repeatable, and easier to manage across environments.

---

## Day 25 – Identity End-to-End

* Implemented Managed Identity for cloud resource access.
* Added Key Vault integration and references.
* Configured Entra ID authentication and authorization.
* Added RBAC assignments for required Azure resources.
* Removed plaintext secrets from application configuration.
* Replaced connection-string based access where possible.

**Outcome:** Authentication and infrastructure access became primarily identity-based instead of secret-based. One Key Vault secret configuration step remained documented as a manual setup item.

---

## Day 26 – Application Insights + KQL

* Added OpenTelemetry instrumentation.
* Connected API and Worker telemetry to Application Insights.
* Enabled distributed tracing across API, Service Bus, Worker, and SQL.
* Created KQL queries for latency analysis, dependency analysis, and error tracking.
* Configured error-rate alerting.
* Verified trace correlation across services.

**Outcome:** Production behavior became observable end-to-end with measurable telemetry and diagnostics.

---

## Day 27 – Security Pass

* Performed a STRIDE-lite threat model review.
* Added API versioning.
* Added request-size limits.
* Added DTO validation at the API boundary.
* Added security headers and ProblemDetails responses.
* Added OpenAPI security documentation.
* Added private endpoint infrastructure for SQL and Key Vault.
* Performed OWASP ZAP baseline testing.
* Fixed identified security findings.
* Verified request validation, versioning, and security headers at runtime.
* Fixed a validation issue discovered during testing that caused malformed requests to return HTTP 500 instead of HTTP 400.

**Outcome:** Improved API security, reduced infrastructure exposure, and validated security controls through testing.

---

## Day 28 – Design Review + ADR

* Reviewed architecture decisions and trade-offs.
* Evaluated strengths and weaknesses of the current design.
* Documented the most important architectural decision.
* Identified reliability risks in the asynchronous workflow.
* Recorded recommended improvements for future iterations.

**Outcome:** Architecture decisions are documented, trade-offs are explicit, and future improvements are clearly identified.

---

# Top Design Review Critique

## Critique Identified During the Review

The strongest critique was around the reliability of the asynchronous messaging workflow.

Order creation currently involves two separate operations:

1. Persisting the order to the database.
2. Publishing a message for downstream processing.

If a failure occurs between those operations, an order can be stored successfully while the message is never delivered.

The review also identified that domain events recorded by the aggregate are not currently driving the messaging workflow, which creates a gap between the intended design and the actual implementation.

A related observation was that the worker processes messages in an at-least-once delivery model, but duplicate message handling is not yet fully addressed.

## Why It Matters

The architecture relies on asynchronous communication to support future growth and service separation.

If messaging is not reliable, failures become difficult to recover from and business workflows can become inconsistent.

Addressing reliability early reduces operational risk and makes future service extraction safer.

## Design Change

The review changed my view of what the next architectural priority should be.

Originally, the focus was on adding more features and additional business domains.

After the review, the priority shifted toward improving reliability by:

* Introducing a transactional outbox pattern for message publishing.
* Making message processing idempotent.
* Aligning the domain-event model with the actual integration workflow.

## Result

The review reinforced that the overall architecture is structurally sound, but reliability is the most important area for improvement.

The current design already separates responsibilities through clean architecture and abstractions, which means these improvements can be implemented without major redesign.

## Demonstrated Evidence

* The solution evolved from a local implementation to Azure-hosted infrastructure without changing core business logic.
* Managed Identity, Key Vault integration, observability, and security hardening were added without requiring changes to domain rules.
* Distributed tracing was verified across API → Service Bus → Worker → SQL.
* Security validation confirmed request limits, API versioning, input validation, hardened responses, and private-networking controls.
* Solution build completed with 0 errors (2 NU1902 advisory warnings).
* Automated test execution passed 4 out of 4 tests.
* Infrastructure validation completed successfully with `az bicep build` returning exit code 0 and no warnings.

These results increased confidence that the architectural decision supports maintainability, extensibility, operational visibility, and future growth while also highlighting reliability as the next area of focus.
