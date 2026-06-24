using System.Security.Claims;
using QuickCart.Application.Abstractions;

namespace QuickCart.Api.Identity;

/// <summary>
/// Reads the caller's identity from the authenticated Entra principal on the current request.
/// When no principal is present (local dev with Entra disabled) it returns a deterministic dev
/// identity, so carts/orders still resolve to a single stable local user without an app registration.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private const string DevObjectId = "dev-local-user";
    private const string DevEmail = "dev@quickcart.local";

    // Entra emits the object id under either the long WS-* URI or the short "oid" claim.
    private static readonly string[] ObjectIdClaims =
    {
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
        "oid",
    };
    private static readonly string[] EmailClaims =
    {
        "preferred_username", ClaimTypes.Email, "email", "emails",
    };

    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string EntraObjectId =>
        IsAuthenticated ? (FirstClaim(ObjectIdClaims) ?? DevObjectId) : DevObjectId;

    public string? Email =>
        IsAuthenticated ? (FirstClaim(EmailClaims) ?? DevEmail) : DevEmail;

    public string? DisplayName =>
        IsAuthenticated ? (FirstClaim(new[] { "name", ClaimTypes.Name }) ?? Email) : "Local Developer";

    private string? FirstClaim(IEnumerable<string> claimTypes)
    {
        var principal = Principal;
        if (principal is null) return null;

        foreach (var type in claimTypes)
        {
            var value = principal.FindFirstValue(type);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}
