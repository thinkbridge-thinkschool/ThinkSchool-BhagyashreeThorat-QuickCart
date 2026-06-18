using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
using QuickCart.Application.Orders;
using QuickCart.Infrastructure;

// Azure SDK messaging tracing (Service Bus send/process spans) is still experimental and
// off by default. Enabling it makes the publish a tracked dependency and the consumer a
// child span — the link that puts API and Worker on one distributed trace. Must be set
// before any ServiceBusClient is created.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// OpenTelemetry → Application Insights. The distro auto-instruments incoming ASP.NET Core
// requests, outbound HttpClient/SqlClient (dependencies), and the Azure SDK (Service Bus),
// and reads the connection string from APPLICATIONINSIGHTS_CONNECTION_STRING. Enabled only
// when that setting exists, so local dev stays free of telemetry noise.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("quickcart-api"))
        .UseAzureMonitor();
}

// Entra ID (Azure AD) app auth. Enabled whenever an AzureAd:ClientId is configured
// (i.e. in the cloud, supplied via app settings). Local dev with no ClientId stays open
// so the in-memory app still runs without an app registration.
var azureAd = builder.Configuration.GetSection("AzureAd");
var entraEnabled = !string.IsNullOrEmpty(azureAd["ClientId"]);
if (entraEnabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAd);

    // Require an authenticated Entra principal on every endpoint by default.
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;
    });
}

// Ordering context wiring (SQL + Service Bus, both via Managed Identity).
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (entraEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
