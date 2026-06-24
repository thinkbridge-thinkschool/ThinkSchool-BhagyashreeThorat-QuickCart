using QuickCart.Domain.Ordering.Entities;
using QuickCart.Domain.Ordering.Enums;
using QuickCart.Domain.Ordering.Events;
using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Ordering.Aggregates;

/// <summary>
/// Ordering aggregate root. Owns its items, enforces its own invariants, and records domain
/// events instead of calling other contexts directly. Orders belong to an authenticated user
/// (<see cref="UserId"/>); item prices are snapshots captured from the Catalog at order time.
/// The cart is edited before checkout, so a placed order is immutable apart from cancellation.
/// </summary>
public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // EF Core
    private Order() { }

    private Order(Guid userId, IEnumerable<OrderItem> items, DateTime utcNow)
    {
        OrderId = Guid.NewGuid();
        UserId = userId;
        Status = OrderStatus.Confirmed;
        _items.AddRange(items);
        TotalAmount = _items.Sum(i => i.LineTotal);
        MarkCreated(utcNow);
    }

    /// <summary>Factory enforcing the "an order must have at least one item" invariant.</summary>
    public static Order Create(Guid userId, IEnumerable<OrderItem> items, DateTime utcNow)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));

        var materialized = items?.ToList() ?? new List<OrderItem>();
        if (materialized.Count == 0)
            throw new InvalidOperationException("An order must contain at least one item.");

        var order = new Order(userId, materialized, utcNow);
        order._domainEvents.Add(new OrderCreatedEvent(order.OrderId, order.UserId, order.TotalAmount, utcNow));
        return order;
    }

    /// <summary>Cancel a placed order. Idempotent.</summary>
    public void Cancel(DateTime utcNow)
    {
        if (Status == OrderStatus.Cancelled) return;
        Status = OrderStatus.Cancelled;
        MarkModified(utcNow);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
