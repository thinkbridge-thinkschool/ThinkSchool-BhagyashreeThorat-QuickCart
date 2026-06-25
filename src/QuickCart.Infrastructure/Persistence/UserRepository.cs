using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Shared.Users;

namespace QuickCart.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly QuickCartDbContext _db;

    public UserRepository(QuickCartDbContext db) => _db = db;

    public Task<User?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) => await _db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
