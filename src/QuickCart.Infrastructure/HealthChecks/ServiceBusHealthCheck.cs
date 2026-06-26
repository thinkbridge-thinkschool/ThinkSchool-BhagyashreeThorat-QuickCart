using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace QuickCart.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the configured Service Bus queue exists and is reachable.
/// Uses ServiceBusAdministrationClient (management plane) so no test messages
/// are sent or consumed during the probe.
/// Registered only when ServiceBus:FullyQualifiedNamespace is configured.
/// </summary>
internal sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusAdministrationClient _admin;
    private readonly string _queueName;

    public ServiceBusHealthCheck(ServiceBusAdministrationClient admin, string queueName)
    {
        _admin = admin;
        _queueName = queueName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _admin.GetQueueRuntimePropertiesAsync(_queueName, cancellationToken);
            var activeMessages = response.Value.ActiveMessageCount;

            return HealthCheckResult.Healthy(
                $"Queue '{_queueName}' reachable. Active messages: {activeMessages}.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Cannot reach Service Bus queue '{_queueName}'.",
                ex);
        }
    }
}
