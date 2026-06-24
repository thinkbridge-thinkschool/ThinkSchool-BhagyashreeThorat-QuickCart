using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly QuickCartDbContext _db;

    public OrderRepository(QuickCartDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken ct = default) => await _db.Orders.AddAsync(order, ct);

    // Owned items are loaded automatically as part of the aggregate.
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
