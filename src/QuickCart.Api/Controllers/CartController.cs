using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Carts;
using QuickCart.Application.Users;
using QuickCart.Contracts.Carts;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
public sealed class CartController : ControllerBase
{
    private readonly CartService _cart;
    private readonly UserService _users;

    public CartController(CartService cart, UserService users)
    {
        _cart = cart;
        _users = users;
    }

    /// <summary>Get the current user's cart.</summary>
    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get(CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        return Ok(ToResponse(await _cart.GetCartAsync(userId, ct)));
    }

    /// <summary>Add a product to the cart (or increase its quantity).</summary>
    [HttpPost("items")]
    public Task<ActionResult<CartResponse>> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        MutateAsync(async userId => await _cart.AddItemAsync(userId, request.ProductId, request.Quantity, ct), ct);

    /// <summary>Set the absolute quantity for a product already in the cart.</summary>
    [HttpPut("items/{productId:guid}")]
    public Task<ActionResult<CartResponse>> UpdateItem(Guid productId, UpdateCartItemRequest request, CancellationToken ct) =>
        MutateAsync(async userId => await _cart.UpdateItemAsync(userId, productId, request.Quantity, ct), ct);

    /// <summary>Remove a product from the cart.</summary>
    [HttpDelete("items/{productId:guid}")]
    public Task<ActionResult<CartResponse>> RemoveItem(Guid productId, CancellationToken ct) =>
        MutateAsync(async userId => await _cart.RemoveItemAsync(userId, productId, ct), ct);

    private async Task<ActionResult<CartResponse>> MutateAsync(Func<Guid, Task<CartView>> mutate, CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        try
        {
            return Ok(ToResponse(await mutate(userId)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid cart operation");
        }
    }

    private async Task<Guid> CurrentUserIdAsync(CancellationToken ct) =>
        (await _users.GetOrProvisionCurrentUserAsync(ct)).UserId;

    private static CartResponse ToResponse(CartView view) => new(
        view.CartId,
        view.Lines
            .Select(l => new CartItemResponse(l.ProductId, l.ProductName, l.ImageUrl, l.UnitPrice, l.Quantity, l.LineTotal, l.IsAvailable))
            .ToList(),
        view.Total);
}
