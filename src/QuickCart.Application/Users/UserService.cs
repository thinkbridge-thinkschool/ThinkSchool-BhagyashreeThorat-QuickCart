using QuickCart.Application.Abstractions;
using QuickCart.Domain.Shared.Users;

namespace QuickCart.Application.Users;

/// <summary>
/// Bridges the authenticated Entra principal to the local User record. On each call it
/// provisions the user on first sight and keeps the profile in sync thereafter — this is the
/// "synchronize or create the user record after login" step. Other use cases (cart, orders)
/// call this to obtain the owning <c>UserId</c> instead of trusting client input.
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;

    public UserService(IUserRepository users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<User> GetOrProvisionCurrentUserAsync(CancellationToken ct = default)
    {
        var oid = _currentUser.EntraObjectId;
        var email = _currentUser.Email ?? $"{oid}@quickcart.local";

        var user = await _users.GetByEntraObjectIdAsync(oid, ct);
        if (user is null)
        {
            user = new User(oid, email, _currentUser.DisplayName, null, DateTime.UtcNow);
            await _users.AddAsync(user, ct);
            await _users.SaveChangesAsync(ct);
            return user;
        }

        user.SyncProfile(email, _currentUser.DisplayName, user.PhoneNumber, DateTime.UtcNow);
        await _users.SaveChangesAsync(ct);
        return user;
    }
}
