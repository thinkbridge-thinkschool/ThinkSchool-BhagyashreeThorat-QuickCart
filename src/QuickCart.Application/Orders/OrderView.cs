namespace QuickCart.Application.Orders;

/// <summary>
/// Read model for an order, with item lines enriched by the current Catalog product name so the
/// frontend can render "My Orders" without extra round-trips. Prices/totals come from the order's
/// own snapshot, not the live catalog.
/// </summary>
public sealed record OrderView(
    Guid OrderId,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderLineView> Items);

public sealed record OrderLineView(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
