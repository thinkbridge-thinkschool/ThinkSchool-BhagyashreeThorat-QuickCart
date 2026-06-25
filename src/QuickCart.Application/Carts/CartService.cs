using QuickCart.Application.Abstractions;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Application.Carts;

/// <summary>
/// Use cases for the shopping cart. Cart operations are keyed by the owning <c>userId</c>
/// (resolved from the authenticated principal at the boundary), never by client-supplied id.
/// Reads enrich the cart with current Catalog prices to build the <see cref="CartView"/>.
/// </summary>
public sealed class CartService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;

    public CartService(ICartRepository carts, IProductRepository products)
    {
        _carts = carts;
        _products = products;
    }

    public async Task<CartView> GetCartAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        return cart is null ? Empty(userId) : await BuildViewAsync(cart, ct);
    }

    public async Task<CartView> AddItemAsync(Guid userId, Guid productId, int quantity, CancellationToken ct = default)
    {
        // Validate against the Catalog before mutating the cart.
        var product = await _products.GetByIdAsync(productId, ct)
            ?? throw new InvalidOperationException("Product does not exist.");
        if (!product.IsAvailable)
            throw new InvalidOperationException("Product is not available.");

        var cart = await GetOrCreateAsync(userId, ct);
        cart.AddItem(productId, quantity, DateTime.UtcNow);
        await _carts.SaveChangesAsync(ct);
        return await BuildViewAsync(cart, ct);
    }

    public async Task<CartView> UpdateItemAsync(Guid userId, Guid productId, int quantity, CancellationToken ct = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct)
            ?? throw new InvalidOperationException("Cart is empty.");
        cart.UpdateItemQuantity(productId, quantity, DateTime.UtcNow);
        await _carts.SaveChangesAsync(ct);
        return await BuildViewAsync(cart, ct);
    }

    public async Task<CartView> RemoveItemAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        if (cart is null) return Empty(userId);

        cart.RemoveItem(productId, DateTime.UtcNow);
        await _carts.SaveChangesAsync(ct);
        return await BuildViewAsync(cart, ct);
    }

    private async Task<Cart> GetOrCreateAsync(Guid userId, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);
        if (cart is not null) return cart;

        cart = Cart.CreateFor(userId, DateTime.UtcNow);
        await _carts.AddAsync(cart, ct);
        return cart;
    }

    private async Task<CartView> BuildViewAsync(Cart cart, CancellationToken ct)
    {
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = (await _products.GetByIdsAsync(productIds, ct)).ToDictionary(p => p.ProductId);

        var lines = cart.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var p);
            var unitPrice = p?.Price ?? 0m;
            return new CartLineView(
                i.ProductId,
                p?.ProductName ?? "(unavailable)",
                p?.ImageUrl,
                unitPrice,
                i.Quantity,
                unitPrice * i.Quantity,
                p?.IsAvailable ?? false);
        }).ToList();

        return new CartView(cart.CartId, cart.UserId, lines, lines.Sum(l => l.LineTotal));
    }

    private static CartView Empty(Guid userId) => new(Guid.Empty, userId, Array.Empty<CartLineView>(), 0m);
}
