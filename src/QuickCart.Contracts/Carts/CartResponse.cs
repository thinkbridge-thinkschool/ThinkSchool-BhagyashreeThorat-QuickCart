namespace QuickCart.Contracts.Carts;

/// <summary>HTTP response shape for a cart, enriched with current prices and line totals.</summary>
public sealed record CartResponse(
    Guid CartId,
    IReadOnlyList<CartItemResponse> Items,
    decimal Total);

public sealed record CartItemResponse(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    bool IsAvailable);
