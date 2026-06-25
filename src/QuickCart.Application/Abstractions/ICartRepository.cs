using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Application.Abstractions;

/// <summary>Persistence boundary for the Cart aggregate. Implemented in Infrastructure.</summary>
public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
