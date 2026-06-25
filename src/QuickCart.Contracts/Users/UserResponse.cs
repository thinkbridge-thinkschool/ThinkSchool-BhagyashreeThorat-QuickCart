namespace QuickCart.Contracts.Users;

/// <summary>HTTP response shape for the current user's profile.</summary>
public sealed record UserResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    string? PhoneNumber);
