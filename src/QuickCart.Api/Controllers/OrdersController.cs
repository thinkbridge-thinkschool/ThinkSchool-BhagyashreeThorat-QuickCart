using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Orders;
using QuickCart.Application.Users;
using QuickCart.Contracts.Orders;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orders;
    private readonly UserService _users;

    public OrdersController(OrderService orders, UserService users)
    {
        _orders = orders;
        _users = users;
    }

    /// <summary>Create an order by checking out the current user's cart.</summary>
    [HttpPost]
    [RequestSizeLimit(16 * 1024)]
    public async Task<ActionResult<OrderResponse>> Checkout(CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        try
        {
            var order = await _orders.CheckoutAsync(userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, ToResponse(order));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Cannot create order");
        }
    }

    /// <summary>Get the current user's orders (most recent first).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMine(CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        var orders = await _orders.GetMyOrdersAsync(userId, ct);
        return Ok(orders.Select(ToResponse).ToList());
    }

    /// <summary>Get one of the current user's orders by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        var order = await _orders.GetByIdAsync(userId, id, ct);
        return order is null ? NotFound() : Ok(ToResponse(order));
    }

    /// <summary>Cancel a placed order.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var userId = await CurrentUserIdAsync(ct);
        try
        {
            var order = await _orders.CancelAsync(userId, id, ct);
            return Ok(ToResponse(order));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Cannot cancel order");
        }
    }

    private async Task<Guid> CurrentUserIdAsync(CancellationToken ct) =>
        (await _users.GetOrProvisionCurrentUserAsync(ct)).UserId;

    private static OrderResponse ToResponse(OrderView order) => new(
        order.OrderId,
        order.UserId,
        order.Status,
        order.TotalAmount,
        order.CreatedAtUtc,
        order.Items
            .Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList());
}
