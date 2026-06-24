using Microsoft.Extensions.DependencyInjection;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Shared.Common;

namespace QuickCart.Infrastructure.Messaging;

/// <summary>
/// Resolves and invokes the registered <see cref="IDomainEventHandler{TEvent}"/>(s) for each
/// recorded domain event. Handlers are looked up by the event's runtime type, so adding a new
/// event + handler requires no change here — just a DI registration.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;

    public DomainEventDispatcher(IServiceProvider services) => _services = services;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in _services.GetServices(handlerType))
            {
                if (handler is null) continue;
                await (Task)handleMethod.Invoke(handler, new object[] { domainEvent, ct })!;
            }
        }
    }
}
