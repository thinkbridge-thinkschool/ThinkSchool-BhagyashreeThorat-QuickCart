using System.Text.Json;
using Azure.Messaging.ServiceBus;
using QuickCart.Application.Abstractions;
using QuickCart.Contracts.Messaging;

namespace QuickCart.Infrastructure.Messaging;

/// <summary>
/// Publishes <see cref="OrderCreatedMessage"/> to the <c>order-events</c> Service Bus queue.
/// The <see cref="ServiceBusSender"/> emits an Activity for the send, and the Azure SDK writes
/// the W3C <c>traceparent</c> into the message — that is what links the Worker's consumer span
/// back to this request. <c>MessageId</c> is the order id, enabling consumer-side de-duplication.
/// </summary>
public sealed class ServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusOrderEventPublisher(ServiceBusClient client, string queueName) =>
        _sender = client.CreateSender(queueName);

    public async Task PublishOrderCreatedAsync(OrderCreatedMessage message, CancellationToken ct = default)
    {
        var busMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
        {
            ContentType = "application/json",
            Subject = nameof(OrderCreatedMessage),
            MessageId = message.OrderId.ToString(),
        };

        await _sender.SendMessageAsync(busMessage, ct);
    }
}
