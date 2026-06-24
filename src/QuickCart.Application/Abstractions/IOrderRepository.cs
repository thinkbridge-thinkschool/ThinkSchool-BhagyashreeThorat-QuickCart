using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Application.Abstractions;

/// <summary>Persistence boundary for the Order aggregate. Implemented in Infrastructure.</summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
