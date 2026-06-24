using QuickCart.Application.Abstractions;
using QuickCart.Contracts.Messaging;

namespace QuickCart.Infrastructure.Messaging;

/// <summary>
/// No-op publisher used in local dev / tests when no Service Bus namespace is configured,
/// so the API still runs end-to-end against the in-memory store without a broker.
/// </summary>
public sealed class NullOrderEventPublisher : IOrderEventPublisher
{
    public Task PublishOrderCreatedAsync(OrderCreatedMessage message, CancellationToken ct = default) => Task.CompletedTask;
}
