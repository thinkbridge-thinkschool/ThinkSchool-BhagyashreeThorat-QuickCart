using QuickCart.Domain.Common;

namespace QuickCart.Domain.Events;

/// <summary>Raised when a new order is submitted. Drives the async "reserve stock / request payment" flow.</summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    DateTime OccurredOnUtc) : IDomainEvent;
