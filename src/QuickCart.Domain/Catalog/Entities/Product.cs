using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Catalog.Entities;

/// <summary>
/// A sellable product. The Catalog is the price authority: Ordering snapshots <see cref="Price"/>
/// and <see cref="ProductName"/> at order time rather than trusting client input.
/// Only an image URL is stored — never binary image data.
/// </summary>
public sealed class Product : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string ProductName { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? ImageUrl { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsAvailable { get; private set; }

    // EF Core
    private Product() => ProductName = string.Empty;

    public Product(
        Guid categoryId,
        string productName,
        string? description,
        decimal price,
        string? imageUrl,
        int stockQuantity,
        DateTime utcNow)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("ProductName is required.", nameof(productName));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (stockQuantity < 0) throw new ArgumentOutOfRangeException(nameof(stockQuantity), "StockQuantity cannot be negative.");

        ProductId = Guid.NewGuid();
        CategoryId = categoryId;
        ProductName = productName;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        StockQuantity = stockQuantity;
        IsAvailable = stockQuantity > 0;
        MarkCreated(utcNow);
    }

    /// <summary>Reduce stock when an order consumes units; flips availability when depleted.</summary>
    public void ReduceStock(int quantity, DateTime utcNow)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (quantity > StockQuantity) throw new InvalidOperationException($"Insufficient stock for product {ProductId}.");

        StockQuantity -= quantity;
        IsAvailable = StockQuantity > 0;
        MarkModified(utcNow);
    }
}
