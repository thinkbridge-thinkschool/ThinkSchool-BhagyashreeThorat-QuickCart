namespace QuickCart.Domain.Ordering.Entities;

/// <summary>A line in a cart. Owned by the Cart aggregate root. Holds no price — the cart
/// references the product; price is resolved from the Catalog at display and at checkout.</summary>
public sealed class CartItem
{
    public Guid CartItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // EF Core
    private CartItem() { }

    public CartItem(Guid productId, int quantity)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        CartItemId = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
    }

    internal void SetQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        Quantity = quantity;
    }

    internal void Increase(int by)
    {
        if (by <= 0) throw new ArgumentOutOfRangeException(nameof(by), "Increment must be positive.");
        Quantity += by;
    }
}
