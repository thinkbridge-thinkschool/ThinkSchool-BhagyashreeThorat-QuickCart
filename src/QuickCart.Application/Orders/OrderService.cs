using QuickCart.Application.Abstractions;
using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Ordering.Entities;

namespace QuickCart.Application.Orders;

/// <summary>
/// Application service for the Ordering use cases. Orders are server-authoritative: the owner is
/// the authenticated <c>userId</c> passed in by the API, and item prices are resolved from the
/// Catalog at order time — never taken from the client. After an order is persisted, its recorded
/// domain events are dispatched (which is what publishes the integration event).
/// </summary>
public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly IDomainEventDispatcher _dispatcher;

    public OrderService(
        IOrderRepository orders,
        ICartRepository carts,
        IProductRepository products,
        IDomainEventDispatcher dispatcher)
    {
        _orders = orders;
        _carts = carts;
        _products = products;
        _dispatcher = dispatcher;
    }

    /// <summary>Create an order from the user's current cart, then clear the cart.</summary>
    public async Task<OrderView> CheckoutAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        if (cart is null || cart.Items.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");

        var items = await ResolveItemsAsync(cart.Items.Select(i => (i.ProductId, i.Quantity)).ToList(), ct);
        var order = Order.Create(userId, items, DateTime.UtcNow);

        await _orders.AddAsync(order, ct);
        cart.Clear(DateTime.UtcNow);
        // One scoped DbContext backs both repositories, so this persists the order and the
        // cleared cart in a single transaction.
        await _orders.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(order.DomainEvents, ct);
        order.ClearDomainEvents();

        return await BuildViewAsync(order, ct);
    }

    public async Task<IReadOnlyList<OrderView>> GetMyOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await _orders.GetByUserAsync(userId, ct);
        var views = new List<OrderView>(orders.Count);
        foreach (var order in orders)
            views.Add(await BuildViewAsync(order, ct));
        return views;
    }

    public async Task<OrderView?> GetByIdAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId) return null;
        return await BuildViewAsync(order, ct);
    }

    /// <summary>Cancel a placed order.</summary>
    public async Task<OrderView> CancelAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId)
            throw new InvalidOperationException("Order not found.");

        order.Cancel(DateTime.UtcNow);
        await _orders.SaveChangesAsync(ct);

        return await BuildViewAsync(order, ct);
    }

    /// <summary>Validate each requested product against the Catalog and snapshot its current price.</summary>
    private async Task<List<OrderItem>> ResolveItemsAsync(
        IReadOnlyCollection<(Guid ProductId, int Quantity)> requested, CancellationToken ct)
    {
        if (requested.Count == 0)
            throw new InvalidOperationException("An order must contain at least one item.");

        var products = (await _products.GetByIdsAsync(requested.Select(r => r.ProductId), ct))
            .ToDictionary(p => p.ProductId);

        var items = new List<OrderItem>(requested.Count);
        foreach (var (productId, quantity) in requested)
        {
            if (!products.TryGetValue(productId, out var product))
                throw new InvalidOperationException($"Product {productId} does not exist.");
            if (!product.IsAvailable)
                throw new InvalidOperationException($"Product '{product.ProductName}' is not available.");

            items.Add(new OrderItem(product.ProductId, product.Price, quantity));
        }
        return items;
    }

    private async Task<OrderView> BuildViewAsync(Order order, CancellationToken ct)
    {
        var products = (await _products.GetByIdsAsync(order.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.ProductId);

        var lines = order.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var p);
            return new OrderLineView(i.ProductId, p?.ProductName ?? "(unknown product)", i.UnitPrice, i.Quantity, i.LineTotal);
        }).ToList();

        return new OrderView(
            order.OrderId,
            order.UserId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            lines);
    }
}
