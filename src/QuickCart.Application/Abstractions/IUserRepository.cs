using QuickCart.Domain.Shared.Users;

namespace QuickCart.Application.Abstractions;

/// <summary>Persistence boundary for the User. Implemented in Infrastructure.</summary>
public interface IUserRepository
{
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
