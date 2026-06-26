using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Application.Abstractions;

/// <summary>Persistence boundary for the Order aggregate. Implemented in Infrastructure.</summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single page of a user's orders, newest first.
    /// COUNT and SELECT are two separate queries — no rows outside the page window are loaded.
    /// </summary>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByUserPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
