using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Infrastructure.Persistence;

public sealed class CartRepository : ICartRepository
{
    private readonly QuickCartDbContext _db;

    public CartRepository(QuickCartDbContext db) => _db = db;

    public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(Cart cart, CancellationToken ct = default) => await _db.Carts.AddAsync(cart, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
