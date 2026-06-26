using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Application.Abstractions;

/// <summary>Read/write boundary for products. Implemented in Infrastructure.</summary>
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Free-text search over product name/description.</summary>
    Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);

    /// <summary>Resolve several products by id — used by checkout to snapshot current prices.</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);

    /// <summary>
    /// Returns a single page of products ordered by name.
    /// Applies optional free-text search and/or category filter before paging.
    /// COUNT and SELECT are two separate queries — no rows are loaded into memory
    /// outside the requested page window.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
