using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using QuickCart.Infrastructure;
using QuickCart.Worker;

// Azure SDK messaging tracing (Service Bus send/process spans) is still experimental and off
// by default. Enabling it makes the consumer's ProcessMessage a span that adopts the trace
// context carried on the message — so the Worker's SQL work nests under the originating API
// request instead of becoming orphaned root operations. Must be set before any client.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

// A minimal web host: App Service (Linux) has no continuous WebJobs, so the Worker runs as a
// web app that binds a port for health probes while the real work happens in a hosted service.
var builder = WebApplication.CreateBuilder(args);

// SQL (DbContext) + Service Bus client, all over Managed Identity — same wiring as the API.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OrderEventsConsumer>();

// OpenTelemetry → Application Insights. The distro auto-instruments the Azure SDK (the Service
// Bus consumer span, restored from the message's traceparent) and SqlClient — which is what
// stitches the Worker's processing into the same distributed trace as the originating request.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("quickcart-worker"))
        .UseAzureMonitor();
}

var app = builder.Build();

app.MapGet("/", () => "QuickCart.Worker is running.");
app.MapGet("/healthz", () => Results.Ok("healthy"));

app.Run();
