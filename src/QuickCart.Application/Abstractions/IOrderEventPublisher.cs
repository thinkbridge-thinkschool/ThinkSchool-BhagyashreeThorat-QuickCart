using QuickCart.Contracts.Messaging;

namespace QuickCart.Application.Abstractions;

/// <summary>
/// Publishes ordering integration events to the messaging transport. Implemented in
/// Infrastructure (Service Bus in the cloud, a no-op locally when no namespace is configured).
/// Called by a domain-event handler, not by the application service directly.
/// </summary>
public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedMessage message, CancellationToken ct = default);
}
