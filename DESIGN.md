# QuickCart — One-Page Design (Day 22 Capstone)

**Product slice:** Checkout & Ordering for QuickCart, a quick-commerce platform that delivers groceries and daily essentials within minutes.
A customer submits an order; the system reserves stock, takes payment, then confirms
and notifies the customer.

**Architecture:** Modular monolith, clean architecture. One deployable (`QuickCart.Api`),
internal modules separated by bounded context. Dependencies point **inward** toward the
Domain; contexts talk to each other through **domain events**, not direct calls — so they
can later be split into services with minimal change.

Each bounded context owns its own domain model, events, and business rules. This keeps
module boundaries explicit, reduces coupling, and aligns the codebase with Domain-Driven
Design principles while remaining a modular monolith.

---

## Bounded contexts

| Context | Responsibility | Status |
|---|---|---|
| **Ordering** | Owns the order lifecycle (submit → pay → confirm/cancel). The slice built now. | ✅ Implemented |
| **Catalog** | Products, descriptions, prices. Source of truth for what can be ordered. | Planned |
| **Inventory** | Stock levels; reserves/releases stock for an order. | Planned |
| **Payment** | Charges the customer, reports success/failure. | Planned |
| **Notifications** | Emails/SMS the customer on order events. | Planned |

Ordering is the core context. The others are supporting and integrate asynchronously.

---

## Core aggregate — `Order`

The aggregate root for the Ordering context. It owns its lines, enforces its own
invariants, and records domain events instead of calling other contexts directly.

- **Root:** `Order` (`Id`, `CustomerId`, `Status`, `CreatedAtUtc`)
- **Owned entity:** `OrderLine` (`ProductId`, `ProductName`, `UnitPrice`, `Quantity`, `LineTotal`) — no identity outside the order
- **Computed:** `Total` = sum of line totals
- **States:** `Submitted → Paid` | `Submitted → Cancelled`

**Invariants enforced inside the aggregate:**
- An order must contain at least one line (`Order.Create`)
- Line `Quantity > 0`, `UnitPrice >= 0`, product/name required
- Cannot pay an order that is not `Submitted`; cannot cancel a `Paid` order

**Behaviour (the only ways to change state):** `Create`, `MarkPaid`, `Cancel`.

`Order` is the **only** entity loaded/saved as a unit and the consistency boundary for a
transaction.

---

## Async flows (event-driven)

The aggregate records domain events; they are dispatched after the order is persisted, and
other contexts react. (Aggregate + events exist today; the dispatcher/handlers are the next
build step.)

```
1. Place order
   POST /api/orders → Order.Create(...) → saved → [OrderCreatedEvent]

2. Reserve & charge  (reacts to OrderCreatedEvent)
   Inventory: reserve stock for the lines
   Payment:   charge the customer → [PaymentSucceededEvent] (or PaymentFailed)

3. Confirm & notify  (reacts to PaymentSucceededEvent)
   Ordering:       Order.MarkPaid()
   Notifications:  send order-confirmation to customer

4. Cancellation flow (planned)

   PaymentFailed or StockUnavailable
           ↓
   Order.Cancel()
           ↓
   Release reservation
           ↓
   Notify customer
```

Failure paths (planned): payment failure / stock shortage → release reservation →
`Order.Cancel()` → notify customer.

---

## Solution layout

```
QuickCart.slnx
├─ src/
│  ├─ QuickCart.Domain          # Bounded-context ownership — no dependencies
│  │   ├─ Ordering              # Core context (implemented)
│  │   │   ├─ Aggregates/Order.cs
│  │   │   ├─ Entities/OrderLine.cs
│  │   │   ├─ Enums/OrderStatus.cs
│  │   │   ├─ Events/OrderCreatedEvent.cs, PaymentSucceededEvent.cs
│  │   │   └─ ValueObjects/
│  │   ├─ Catalog               # Entities/, Events/        (planned)
│  │   ├─ Inventory             # Entities/, Events/        (planned)
│  │   ├─ Payments              # Entities/, Events/        (planned)
│  │   ├─ Notifications         # Entities/                 (planned)
│  │   └─ Shared                # Cross-context building blocks
│  │       ├─ Common/IDomainEvent.cs
│  │       ├─ Abstractions/
│  │       └─ Exceptions/
│  ├─ QuickCart.Application      # Use cases + ports        → Domain
│  │   ├─ Abstractions/IOrderRepository.cs
│  │   └─ Orders/OrderService.cs
│  ├─ QuickCart.Contracts        # HTTP request/response DTOs (API boundary shapes)
│  │   └─ Orders/CreateOrderRequest.cs, OrderResponse.cs
│  ├─ QuickCart.Infrastructure   # EF Core persistence, DI  → Application, Domain
│  │   ├─ Persistence/QuickCartDbContext.cs, OrderRepository.cs
│  │   └─ DependencyInjection.cs
│  └─ QuickCart.Api              # Controllers, composition root → Application, Contracts, Infrastructure
│      ├─ Controllers/OrdersController.cs
│      └─ Program.cs
└─ tests/
   └─ QuickCart.Tests            # xUnit — Order aggregate tests
```

**Dependency rule:** Domain depends on nothing. Application depends only on Domain.
Infrastructure and Api depend inward. `Contracts` is the public API shape, kept separate
from domain types; the Application layer works directly with domain objects, so there are
no redundant Application DTOs.

**Endpoints today:** `POST /api/orders`, `GET /api/orders/{id}`.
**Persistence today:** EF Core 10 (InMemory provider; swappable for SQL Server in `AddInfrastructure`).
