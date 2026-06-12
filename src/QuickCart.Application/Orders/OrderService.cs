using QuickCart.Application.Abstractions;
using QuickCart.Domain.Aggregates;

namespace QuickCart.Application.Orders;

/// <summary>
/// Application service for the Ordering use cases. Orchestrates the aggregate and the
/// repository; it works directly with domain types, so there are no separate Application DTOs.
/// </summary>
public sealed class OrderService
{
    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders) => _orders = orders;

    public async Task<Order> CreateOrderAsync(Guid customerId, IEnumerable<OrderLine> lines, CancellationToken ct = default)
    {
        var order = Order.Create(customerId, lines, DateTime.UtcNow);
        await _orders.AddAsync(order, ct);
        await _orders.SaveChangesAsync(ct);
        return order;
    }

    public Task<Order?> GetAsync(Guid id, CancellationToken ct = default) => _orders.GetByIdAsync(id, ct);
}
