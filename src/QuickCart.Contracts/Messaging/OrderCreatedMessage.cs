namespace QuickCart.Contracts.Messaging;

/// <summary>
/// Integration event carried over Service Bus when an order is submitted. This is the wire
/// contract shared by the API (publisher) and the Worker (consumer) — distinct from the
/// in-process domain event, so the transport schema can evolve independently.
/// </summary>
public sealed record OrderCreatedMessage(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    DateTime CreatedAtUtc);
