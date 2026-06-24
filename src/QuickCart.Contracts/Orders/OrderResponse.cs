namespace QuickCart.Contracts.Orders;

/// <summary>HTTP response shape for an order.</summary>
public sealed record OrderResponse(
    Guid OrderId,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
