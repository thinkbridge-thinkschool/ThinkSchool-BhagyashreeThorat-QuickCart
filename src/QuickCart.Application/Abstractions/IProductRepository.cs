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

    Task SaveChangesAsync(CancellationToken ct = default);
}
