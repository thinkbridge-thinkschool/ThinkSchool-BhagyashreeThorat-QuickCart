namespace QuickCart.Application.Abstractions;

/// <summary>
/// The identity of the caller, read from the authenticated Entra principal at the API boundary.
/// Implemented in the API over <c>HttpContext</c>. Locally (no Entra configured) it yields a
/// deterministic dev identity so the happy path still runs end-to-end without an app registration.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The immutable Entra object id ("oid"), or a fixed dev id when running locally.</summary>
    string EntraObjectId { get; }

    string? Email { get; }
    string? DisplayName { get; }

    /// <summary>True when backed by a real authenticated Entra principal.</summary>
    bool IsAuthenticated { get; }
}
