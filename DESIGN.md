# QuickCart — Design (Day 22 → Day 29)

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
  Beverages, Snacks, Electronics.
- **`Product`** (`ProductId`, `CategoryId`, `ProductName`, `Description`, `Price`, `ImageUrl`,
  `StockQuantity`, `IsAvailable`). Products belong to a category. **Image URLs only** — no
  binaries in SQL; the frontend uses Angular assets or Blob Storage URLs.

Catalog is the price authority: when an order is placed, the Ordering context resolves the
current `Price` and `ProductName` from here rather than trusting client input.

---

## Shared / User

- **`User`** (`UserId`, `EntraObjectId`, `Email`, `DisplayName`, `PhoneNumber`).
- Users authenticate through Microsoft / Entra ID. On first authenticated request the user is
  **synced/created locally** from the token's object-id and profile claims (`EntraObjectId` is
  the stable link). Orders and carts belong to the authenticated `UserId` — the API never
  accepts a customer id, name, or email in a request body.

---

## Core aggregate — `Order`

The aggregate root for the Ordering context. It owns its items, enforces its own invariants,
and records domain events instead of calling other contexts directly.

- **Root:** `Order` (`OrderId`, `UserId`, `Status`, `TotalAmount`, `EditableUntilUtc`,
  `CreatedAtUtc`)
- **Owned entity:** `OrderItem` (`OrderItemId`, `ProductId`, `UnitPrice`, `Quantity`) — a
  product line within an order; the `UnitPrice` is a **snapshot captured at order time** so a
  later catalog price change does not rewrite history. No identity outside the order.
- **Computed:** line total = `UnitPrice * Quantity`; `TotalAmount` is the persisted sum.
- **Edit window:** an order may be modified for ~3 minutes after creation
  (`EditableUntilUtc = CreatedAtUtc + 3m`); after that it is locked.

**Statuses:** `Pending → Processing → Paid → Delivered`, with `Cancelled` reachable from the
pre-delivery states.

**Invariants enforced inside the aggregate:**
- An order must contain at least one item.
- Item `Quantity > 0`, `UnitPrice >= 0`.
- Items can only be changed while `utcNow <= EditableUntilUtc` and status is `Pending`.
- A `Delivered` order cannot be cancelled; a `Paid`/`Delivered` order cannot be re-paid.

**Behaviour (the only ways to change state):** `Create`, `UpdateItems` (within the window),
`MarkProcessing`, `MarkPaid`, `MarkDelivered`, `Cancel`.

`Order` is the **only** entity loaded/saved as a unit and the consistency boundary for a
transaction.

---

## Cart aggregate (Ordering)

- **Root:** `Cart` (`CartId`, `UserId`) — one active cart per user.
- **Owned entity:** `CartItem` (`CartItemId`, `ProductId`, `Quantity`).
- **Behaviour:** `AddItem`, `UpdateItemQuantity`, `RemoveItem`, `Clear`. Checkout reads the
  cart, resolves prices from Catalog, creates the `Order`, and clears the cart.

---

## Async flows (event-driven)

The aggregate records domain events; after the order is persisted, a **domain-event dispatcher**
translates them into integration events on Service Bus, and other contexts/the Worker react.

> **Day 29 reliability fix (Day 28 review item):** previously the application service
> hand-built the integration message, bypassing the recorded domain events. That gap is now
> closed — `OrderCreatedEvent` is dispatched to an application handler which publishes the
> `OrderCreatedMessage`. (A transactional outbox + consumer idempotency remain documented
> future work for full at-least-once safety.)

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

## Application features

- **Categories:** Get Categories
- **Products:** Get Products, Get Product By Id, Search Products, Filter By Category
- **Cart:** Get Cart, Add To Cart, Update Cart Item, Remove Cart Item
- **Orders:** Create Order (checkout), Get My Orders, Get Order By Id, Update Order (within edit window)

---

## Solution layout

```
QuickCart.slnx
├─ src/
│  ├─ QuickCart.Domain          # Bounded-context ownership — no dependencies
│  │   ├─ Catalog               # Category, Product
│  │   ├─ Ordering              # Order (+ OrderItem), Cart (+ CartItem), events, enums
│  │   ├─ Shared                # BaseEntity, IDomainEvent, User
│  │   ├─ Inventory / Payments  # Future scope
│  ├─ QuickCart.Application      # Use cases + ports        → Domain
│  ├─ QuickCart.Contracts        # HTTP request/response DTOs + integration messages
│  ├─ QuickCart.Infrastructure   # EF Core persistence, messaging, DI → Application, Domain
│  └─ QuickCart.Api              # Controllers, composition root → Application, Contracts, Infrastructure
├─ web/                          # Angular frontend (product listing, cart, checkout, orders)
└─ tests/
   └─ QuickCart.Tests            # xUnit — aggregate tests
```

**Dependency rule:** Domain depends on nothing. Application depends only on Domain.
Infrastructure and Api depend inward. `Contracts` is the public API/wire shape, kept separate
from domain types.

**Persistence:** EF Core 10. SQL Server in the cloud (via Managed Identity); InMemory provider
locally / in tests. One migration describes the full model.
```
