using QuickCart.Domain.Shared.Common;

namespace QuickCart.Domain.Ordering.Events;

/// <summary>Raised when payment for an order has cleared. Drives the async "confirm order / notify customer" flow.</summary>
public sealed record PaymentSucceededEvent(
    Guid OrderId,
    decimal AmountPaid,
    DateTime OccurredOnUtc) : IDomainEvent;
