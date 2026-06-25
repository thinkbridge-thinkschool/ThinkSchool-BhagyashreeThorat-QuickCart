using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using QuickCart.Application.Abstractions;
using QuickCart.Contracts.Messaging;

namespace QuickCart.Worker;

/// <summary>
/// Background consumer for the <c>order-events</c> queue. For each OrderCreated message it
/// loads the order from SQL and advances it to Paid — the "process payment" half of the flow.
/// Because the Azure SDK restores the trace context carried on the message, the work done
/// here (including the SQL calls) appears as a linked span under the original API request.
/// </summary>
public sealed class OrderEventsConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderEventsConsumer> _logger;
    private readonly string _queueName;
    private ServiceBusProcessor? _processor;

    public OrderEventsConsumer(
        ServiceBusClient client,
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<OrderEventsConsumer> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;
        _queueName = configuration["ServiceBus:QueueName"] ?? "order-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false,
        });

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Listening on queue {Queue}", _queueName);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message.Body.ToObjectFromJson<OrderCreatedMessage>()
            ?? throw new InvalidOperationException("Empty OrderCreatedMessage body.");

        _logger.LogInformation("Processing order {OrderId} (total {Total})", message.OrderId, message.TotalAmount);

        // A fresh DI scope per message → a fresh DbContext, mirroring a web request's lifetime.
        await using var scope = _services.CreateAsyncScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // The current scope has no payment gateway — the order is already Confirmed when placed.
        // The Worker receives the integration event for observability (distributed tracing) and
        // future extension (notifications, inventory reservation) without changing order status.
        _logger.LogInformation("Order {OrderId} received for processing (total {Total}). No further status transition in current scope.",
            message.OrderId, message.TotalAmount);

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error from {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
