using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Application.Abstractions;

/// <summary>
/// Publishes ordering integration events to the messaging transport. Implemented in
/// Infrastructure (Service Bus in the cloud, a no-op locally when no namespace is configured).
/// </summary>
public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(Order order, CancellationToken ct = default);
}
