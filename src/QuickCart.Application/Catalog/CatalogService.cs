using QuickCart.Application.Abstractions;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Application.Catalog;

/// <summary>
/// Read use-cases for the Catalog context: list categories, browse/search/filter products.
/// Works directly with domain types; the API maps them to response DTOs at the boundary.
/// </summary>
public sealed class CatalogService
{
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;

    public CatalogService(ICategoryRepository categories, IProductRepository products)
    {
        _categories = categories;
        _products = products;
    }

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        _categories.GetAllAsync(ct);

    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default) =>
        _products.GetAllAsync(ct);

    public Task<Product?> GetProductAsync(Guid productId, CancellationToken ct = default) =>
        _products.GetByIdAsync(productId, ct);

    public Task<IReadOnlyList<Product>> SearchProductsAsync(string? term, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(term)
            ? _products.GetAllAsync(ct)
            : _products.SearchAsync(term.Trim(), ct);

    public Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        _products.GetByCategoryAsync(categoryId, ct);
}
