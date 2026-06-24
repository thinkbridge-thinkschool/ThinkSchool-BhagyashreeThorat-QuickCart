using QuickCart.Domain.Shared.Common;

namespace QuickCart.Application.Abstractions;

/// <summary>
/// Dispatches the domain events an aggregate recorded to their handlers, after the aggregate has
/// been persisted. This is the seam that turns recorded domain events into side effects (e.g. an
/// integration event on Service Bus) instead of the application service publishing directly.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
}
