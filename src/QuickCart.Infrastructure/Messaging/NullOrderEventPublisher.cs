using QuickCart.Application.Abstractions;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Infrastructure.Messaging;

/// <summary>
/// No-op publisher used in local dev / tests when no Service Bus namespace is configured,
/// so the API still runs end-to-end against the in-memory store without a broker.
/// </summary>
public sealed class NullOrderEventPublisher : IOrderEventPublisher
{
    public Task PublishOrderCreatedAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
}
