using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Ordering.Events;

/// <summary>Raised when a new order is submitted. Drives the async "process / request payment" flow.</summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    DateTime OccurredOnUtc) : IDomainEvent;
