using QuickCart.Domain.Common;
using QuickCart.Domain.Events;

namespace QuickCart.Domain.Aggregates;

public enum OrderStatus
{
    Submitted,
    Paid,
    Cancelled
}

/// <summary>A single line on an order. Owned by the <see cref="Order"/> aggregate root.</summary>
public sealed class OrderLine
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    // EF Core
    private OrderLine() => ProductName = string.Empty;

    public OrderLine(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("ProductName is required.", nameof(productName));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}

/// <summary>
/// Ordering aggregate root. Owns its lines, enforces its own invariants, and records
/// domain events instead of calling other contexts directly.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // EF Core
    private Order() { }

    private Order(Guid id, Guid customerId, IEnumerable<OrderLine> lines, DateTime createdAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        CreatedAtUtc = createdAtUtc;
        Status = OrderStatus.Submitted;
        _lines.AddRange(lines);
    }

    /// <summary>Factory enforcing the "an order must have at least one line" invariant.</summary>
    public static Order Create(Guid customerId, IEnumerable<OrderLine> lines, DateTime utcNow)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));

        var materialized = lines?.ToList() ?? new List<OrderLine>();
        if (materialized.Count == 0)
            throw new InvalidOperationException("An order must contain at least one line.");

        var order = new Order(Guid.NewGuid(), customerId, materialized, utcNow);
        order._domainEvents.Add(new OrderCreatedEvent(order.Id, order.CustomerId, order.Total, utcNow));
        return order;
    }

    public void MarkPaid(DateTime utcNow)
    {
        if (Status != OrderStatus.Submitted)
            throw new InvalidOperationException($"Cannot pay an order in status '{Status}'.");

        Status = OrderStatus.Paid;
        _domainEvents.Add(new PaymentSucceededEvent(Id, Total, utcNow));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Paid)
            throw new InvalidOperationException("A paid order cannot be cancelled.");

        Status = OrderStatus.Cancelled;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
