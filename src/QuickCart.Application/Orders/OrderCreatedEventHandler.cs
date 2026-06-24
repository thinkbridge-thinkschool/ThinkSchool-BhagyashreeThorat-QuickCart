using QuickCart.Application.Abstractions;
using QuickCart.Contracts.Messaging;
using QuickCart.Domain.Ordering.Events;

namespace QuickCart.Application.Orders;

/// <summary>
/// Translates the in-process <see cref="OrderCreatedEvent"/> into the <see cref="OrderCreatedMessage"/>
/// integration event and publishes it. This closes the Day 28 gap where the application service
/// published directly and the recorded domain event was never used.
/// </summary>
public sealed class OrderCreatedEventHandler : IDomainEventHandler<OrderCreatedEvent>
{
    private readonly IOrderEventPublisher _publisher;

    public OrderCreatedEventHandler(IOrderEventPublisher publisher) => _publisher = publisher;

    public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken ct = default)
    {
        var message = new OrderCreatedMessage(
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.TotalAmount,
            domainEvent.OccurredOnUtc);

        return _publisher.PublishOrderCreatedAsync(message, ct);
    }
}
