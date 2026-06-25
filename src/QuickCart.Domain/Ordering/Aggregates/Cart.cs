using QuickCart.Domain.Ordering.Entities;
using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Ordering.Aggregates;

/// <summary>
/// A user's active shopping cart. Aggregate root that owns its items. One active cart per user.
/// Holds only product references and quantities; prices live in the Catalog and are resolved at
/// display/checkout, so the cart never goes stale on a price change.
/// </summary>
public sealed class Cart : BaseEntity
{
    private readonly List<CartItem> _items = new();

    public Guid CartId { get; private set; }
    public Guid UserId { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    // EF Core
    private Cart() { }

    private Cart(Guid userId, DateTime utcNow)
    {
        CartId = Guid.NewGuid();
        UserId = userId;
        MarkCreated(utcNow);
    }

    public static Cart CreateFor(Guid userId, DateTime utcNow)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        return new Cart(userId, utcNow);
    }

    /// <summary>Add a product, or increase its quantity if already in the cart.</summary>
    public void AddItem(Guid productId, int quantity, DateTime utcNow)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
            _items.Add(new CartItem(productId, quantity));
        else
            existing.Increase(quantity);

        MarkModified(utcNow);
    }

    /// <summary>Set the absolute quantity for a product already in the cart.</summary>
    public void UpdateItemQuantity(Guid productId, int quantity, DateTime utcNow)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException("Product is not in the cart.");
        item.SetQuantity(quantity);
        MarkModified(utcNow);
    }

    public void RemoveItem(Guid productId, DateTime utcNow)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;
        _items.Remove(item);
        MarkModified(utcNow);
    }

    public void Clear(DateTime utcNow)
    {
        if (_items.Count == 0) return;
        _items.Clear();
        MarkModified(utcNow);
    }
}
