namespace QuickCart.Application.Carts;

/// <summary>
/// Read model for a cart, enriched with current Catalog data (name, price, line total) so the
/// frontend can render and total the cart without a second round-trip. Distinct from the
/// persisted <c>Cart</c> aggregate, which stores only product references and quantities.
/// </summary>
public sealed record CartView(
    Guid CartId,
    Guid UserId,
    IReadOnlyList<CartLineView> Lines,
    decimal Total);

public sealed record CartLineView(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    bool IsAvailable);
