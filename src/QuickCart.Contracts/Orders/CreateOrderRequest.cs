namespace QuickCart.Contracts.Orders;

/// <summary>HTTP request body for creating an order. This is the public API boundary shape,
/// deliberately decoupled from the domain model.</summary>
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderLineDto> Lines);

public sealed record CreateOrderLineDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
