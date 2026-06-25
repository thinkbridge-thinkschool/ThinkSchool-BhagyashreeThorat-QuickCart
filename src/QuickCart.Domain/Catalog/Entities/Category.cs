using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Catalog.Entities;

/// <summary>
/// A product category (e.g. Grocery, Beverages). Catalog is a supporting context — reference
/// data the Ordering context reads from, so the model is intentionally light.
/// </summary>
public sealed class Category : BaseEntity
{
    public Guid CategoryId { get; private set; }
    public string CategoryName { get; private set; }
    public string? Description { get; private set; }

    // EF Core
    private Category() => CategoryName = string.Empty;

    public Category(string categoryName, string? description, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("CategoryName is required.", nameof(categoryName));

        CategoryId = Guid.NewGuid();
        CategoryName = categoryName;
        Description = description;
        MarkCreated(utcNow);
    }
}
