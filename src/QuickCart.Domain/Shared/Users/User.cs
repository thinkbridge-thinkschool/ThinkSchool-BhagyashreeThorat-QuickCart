using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Shared.Users;

/// <summary>
/// A QuickCart user. Identity is owned by Microsoft / Entra ID; the local record is a
/// projection synced from the token. <see cref="EntraObjectId"/> (the immutable "oid" claim)
/// is the stable link — never the email, which can change. Orders and carts belong to
/// <see cref="UserId"/>.
/// </summary>
public sealed class User : BaseEntity
{
    public Guid UserId { get; private set; }
    public string EntraObjectId { get; private set; }
    public string Email { get; private set; }
    public string? DisplayName { get; private set; }
    public string? PhoneNumber { get; private set; }

    // EF Core
    private User()
    {
        EntraObjectId = string.Empty;
        Email = string.Empty;
    }

    public User(string entraObjectId, string email, string? displayName, string? phoneNumber, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(entraObjectId))
            throw new ArgumentException("EntraObjectId is required.", nameof(entraObjectId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        UserId = Guid.NewGuid();
        EntraObjectId = entraObjectId;
        Email = email;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        MarkCreated(utcNow);
    }

    /// <summary>Refresh mutable profile fields from the latest token claims. No-op if unchanged.</summary>
    public void SyncProfile(string email, string? displayName, string? phoneNumber, DateTime utcNow)
    {
        if (email == Email && displayName == DisplayName && phoneNumber == PhoneNumber)
            return;

        if (!string.IsNullOrWhiteSpace(email)) Email = email;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        MarkModified(utcNow);
    }
}
