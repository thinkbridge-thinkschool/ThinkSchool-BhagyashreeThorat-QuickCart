using QuickCart.Domain.Shared.Common;

namespace QuickCart.Application.Abstractions;

/// <summary>Handles a domain event after its aggregate has been persisted.</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
