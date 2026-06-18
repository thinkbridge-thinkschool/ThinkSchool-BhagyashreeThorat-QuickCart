using System.Text.Json;
using Azure.Messaging.ServiceBus;
using QuickCart.Application.Abstractions;
using QuickCart.Contracts.Messaging;
using QuickCart.Domain.Ordering.Aggregates;

namespace QuickCart.Infrastructure.Messaging;

/// <summary>
/// Publishes <see cref="OrderCreatedMessage"/> to the <c>order-events</c> Service Bus queue.
/// The <see cref="ServiceBusSender"/> emits an Activity for the send, and the Azure SDK
/// writes the W3C <c>traceparent</c> into the message's application properties /
/// Diagnostic-Id — that is what links the Worker's consumer span back to this request.
/// </summary>
public sealed class ServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusOrderEventPublisher(ServiceBusClient client, string queueName) =>
        _sender = client.CreateSender(queueName);

    public async Task PublishOrderCreatedAsync(Order order, CancellationToken ct = default)
    {
        var payload = new OrderCreatedMessage(order.Id, order.CustomerId, order.Total, order.CreatedAtUtc);

        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(payload))
        {
            ContentType = "application/json",
            Subject = nameof(OrderCreatedMessage),
            MessageId = order.Id.ToString(),
        };

        await _sender.SendMessageAsync(message, ct);
    }
}
