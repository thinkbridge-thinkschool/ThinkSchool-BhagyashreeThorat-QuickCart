using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Orders;
using QuickCart.Contracts.Orders;
using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Ordering.Entities;

namespace QuickCart.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orders;

    public OrdersController(OrderService orders) => _orders = orders;

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var lines = request.Lines.Select(l => new OrderLine(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity));

        try
        {
            var order = await _orders.CreateOrderAsync(request.CustomerId, lines, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        return order is null ? NotFound() : Ok(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.Total,
        order.Lines
            .Select(l => new OrderLineResponse(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal))
            .ToList(),
        order.CreatedAtUtc);
}
