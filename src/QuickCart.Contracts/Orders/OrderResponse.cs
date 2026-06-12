namespace QuickCart.Contracts.Orders;

/// <summary>HTTP response shape for an order.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineResponse> Lines,
    DateTime CreatedAtUtc);

public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
