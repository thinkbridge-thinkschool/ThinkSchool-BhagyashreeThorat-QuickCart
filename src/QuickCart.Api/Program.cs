using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
using QuickCart.Api.OpenApi;
using QuickCart.Application.Orders;
using QuickCart.Infrastructure;

// Azure SDK messaging tracing (Service Bus send/process spans) is still experimental and
// off by default. Enabling it makes the publish a tracked dependency and the consumer a
// child span — the link that puts API and Worker on one distributed trace. Must be set
// before any ServiceBusClient is created.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

// Request size limits (DoS hardening). The global Kestrel cap is the backstop for every
// endpoint; actions tighten it further with [RequestSizeLimit]. AddServerHeader=false stops
// Kestrel advertising its name/version (information disclosure).
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1 MB
    options.AddServerHeader = false;
});

// OpenAPI with the Entra bearer scheme described in the document (auth is enforced by the
// FallbackPolicy below; the transformer makes that contract visible to clients).
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddControllers();

// RFC 7807 ProblemDetails for error responses, including unhandled exceptions surfaced via
// UseExceptionHandler — so no raw exception text or stack trace ever leaks to the caller.
builder.Services.AddProblemDetails();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc();

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

// Any unhandled exception becomes a ProblemDetails response (no stack trace leaks).
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Tell browsers to use HTTPS only (HSTS). Dev is excluded so localhost over http works.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Baseline security response headers on every response. Addresses the common OWASP ZAP
// baseline findings (missing CSP, X-Content-Type-Options, anti-clickjacking, etc.). The API
// returns JSON only, so a deny-all CSP is safe and there is no UI to break.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    await next();
});

if (entraEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
