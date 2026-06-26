using QuickCart.Application.Abstractions;
using QuickCart.Domain.Catalog.Entities;
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

        // ResolveItemsAsync already loads the products; reuse that dictionary so
        // BuildView does not make a second round-trip to the database.
        var (items, products) = await ResolveItemsAsync(
            cart.Items.Select(i => (i.ProductId, i.Quantity)).ToList(), ct);

        var order = Order.Create(userId, items, DateTime.UtcNow);

        await _orders.AddAsync(order, ct);
        cart.Clear(DateTime.UtcNow);
        // One scoped DbContext backs both repositories, so this persists the order and the
        // cleared cart in a single transaction.
        await _orders.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(order.DomainEvents, ct);
        order.ClearDomainEvents();

        return BuildView(order, products);
    }

    /// <summary>
    /// Return the current user's orders, newest first.
    /// Performance: a single product batch query is made for all orders combined,
    /// replacing the previous N per-order queries (N+1 problem).
    /// </summary>
    public async Task<IReadOnlyList<OrderView>> GetMyOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await _orders.GetByUserAsync(userId, ct);
        if (orders.Count == 0) return Array.Empty<OrderView>();

        var allProductIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId));
        var products = (await _products.GetByIdsAsync(allProductIds, ct))
            .ToDictionary(p => p.ProductId);

        return orders.Select(o => BuildView(o, products)).ToList();
    }

    /// <summary>
    /// Returns a page of the current user's orders, newest first, with pagination metadata.
    /// Reuses the same batch product lookup as <see cref="GetMyOrdersAsync"/> — no N+1.
    /// </summary>
    public async Task<(IReadOnlyList<OrderView> Items, int TotalCount)> GetMyOrdersPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var (orders, total) = await _orders.GetByUserPagedAsync(userId, page, pageSize, ct);

        if (orders.Count == 0) return (Array.Empty<OrderView>(), total);

        var allProductIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId));
        var products = (await _products.GetByIdsAsync(allProductIds, ct))
            .ToDictionary(p => p.ProductId);

        return (orders.Select(o => BuildView(o, products)).ToList(), total);
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

    /// <summary>
    /// Validate each requested product against the Catalog and snapshot its current price.
    /// Returns both the order items and the loaded product dictionary so the caller can
    /// reuse it for BuildView without an extra database round-trip.
    /// </summary>
    private async Task<(List<OrderItem> items, Dictionary<Guid, Product> products)> ResolveItemsAsync(
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
        return (items, products);
    }

    // Used by single-order operations (GetByIdAsync, CancelAsync) where a batch is not needed.
    private async Task<OrderView> BuildViewAsync(Order order, CancellationToken ct)
    {
        var products = (await _products.GetByIdsAsync(order.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.ProductId);
        return BuildView(order, products);
    }

    private static OrderView BuildView(Order order, Dictionary<Guid, Product> products)
    {
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
