namespace QuickCart.Domain.Ordering.Entities;

/// <summary>
/// A product line within an order. Owned by the Order aggregate root. <see cref="UnitPrice"/>
/// is a snapshot captured from the Catalog at order time, so a later price change never rewrites
/// order history. The product's display name is intentionally not stored here (per the entity's
/// field set); read paths enrich it from the Catalog when needed.
/// </summary>
public sealed class OrderItem
{
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    // EF Core
    private OrderItem() { }

    public OrderItem(Guid productId, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        OrderItemId = Guid.NewGuid();
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
