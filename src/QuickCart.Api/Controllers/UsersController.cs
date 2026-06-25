using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Users;
using QuickCart.Contracts.Users;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users) => _users = users;

    /// <summary>Return the current user, provisioning/syncing the local record from the token on first sight.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var user = await _users.GetOrProvisionCurrentUserAsync(ct);
        return Ok(new UserResponse(user.UserId, user.Email, user.DisplayName, user.PhoneNumber));
    }
}
