using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Application.Abstractions;

/// <summary>Read/write boundary for categories. Implemented in Infrastructure.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken ct = default);
}
