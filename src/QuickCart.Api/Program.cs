using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
using QuickCart.Api.HealthChecks;
using QuickCart.Api.OpenApi;
using QuickCart.Api.Identity;
using QuickCart.Application.Abstractions;
using QuickCart.Application.Carts;
using QuickCart.Application.Catalog;
using QuickCart.Application.Orders;
using QuickCart.Application.Users;
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

    // Require the delegated API scope on every endpoint by default — not merely an
    // authenticated principal. This is the scope-based authorization the Day 27 threat model
    // listed as future work. The scope name is config-driven (AzureAd:Scopes), default access_as_user.
    var requiredScopes = (azureAd["Scopes"] ?? "access_as_user")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireScope(requiredScopes)
            .Build();
    });
}

// Ordering context wiring (SQL + Service Bus, both via Managed Identity).
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks: /health (liveness) and /health/ready (readiness).
// Infrastructure checks (DB + optional Service Bus) are registered in the
// Infrastructure layer so Program.cs stays free of provider-specific logic.
builder.Services
    .AddHealthChecks()
    .AddInfrastructureHealthChecks(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CartService>();

// CORS policies.
// Dev: Angular dev server on localhost.
// Prod: Angular SWA — origin is injected via Bicep app setting (Cors__AllowedOrigin).
const string DevCorsPolicy  = "AngularDev";
const string ProdCorsPolicy = "SwaOrigin";
var corsAllowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "";

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());

    if (!string.IsNullOrEmpty(corsAllowedOrigin))
    {
        options.AddPolicy(ProdCorsPolicy, policy => policy
            .WithOrigins(corsAllowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
    }
});

var app = builder.Build();

// Seed a sample catalog on first run (idempotent — no-ops if categories already exist).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuickCart.Infrastructure.Persistence.QuickCartDbContext>();
    await QuickCart.Infrastructure.Persistence.CatalogSeeder.SeedAsync(db);
}

// Any unhandled exception becomes a ProblemDetails response (no stack trace leaks).
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Serve the OpenAPI document (/openapi/v1.json) and an interactive Swagger UI on top of
    // it at /swagger. Dev-only: production stays free of an exposed endpoint explorer.
    // AllowAnonymous: the global FallbackPolicy would otherwise require a token to fetch the
    // document itself, so Swagger UI's spec fetch would fail with 401 before you can sign in.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "QuickCart API v1");
        options.RoutePrefix = "swagger";
    });

    // Allow the Angular dev server to call the API in local development.
    app.UseCors(DevCorsPolicy);
}
else
{
    // Tell browsers to use HTTPS only (HSTS). Dev is excluded so localhost over http works.
    app.UseHsts();

    // Allow the Angular SWA to call the API from a different origin.
    if (!string.IsNullOrEmpty(corsAllowedOrigin))
        app.UseCors(ProdCorsPolicy);
}

app.UseHttpsRedirection();

// Security headers — must come before UseStaticFiles so the headers are on every response.
// CSP is split by path: the JSON API needs no sub-resources (deny-all), while the Angular
// SPA needs same-origin scripts/styles plus the Microsoft login domain for MSAL.
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        // JSON API: deny all sub-resources and prevent response caching.
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        h["Cache-Control"] = "no-store";
    }
    else
    {
        // Angular SPA: same-origin resources + Microsoft login endpoints for MSAL.
        // script-src 'unsafe-inline' is required for Angular's beasties deferred-CSS loader:
        //   <link rel="stylesheet" media="print" onload="this.media='all'">
        // Without it the onload handler (an inline event handler) is blocked by CSP and the
        // external stylesheet stays locked to media="print", stripping all component styles.
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "connect-src 'self' https://login.microsoftonline.com; " +
            "frame-ancestors 'none'";
    }

    await next();
});

// Serve the Angular SPA from wwwroot. UseDefaultFiles maps "/" → index.html,
// UseStaticFiles serves the bundled JS/CSS/assets.
app.UseDefaultFiles();
app.UseStaticFiles();

if (entraEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Health endpoints are intentionally anonymous.
// The global FallbackPolicy requires a JWT; probes (App Service, monitoring tools,
// deployment pipelines) have no token and must never be blocked.
//
// /health        – liveness:  is the process alive and serving HTTP?
//                  Runs no dependency checks; always Healthy if the process responds.
//
// /health/ready  – readiness: can the service handle real traffic?
//                  Runs DB and Service Bus checks (tagged "ready").
//                  Returns 503 if the database is unreachable.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate      = _ => false,  // liveness: no dependency checks
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate      = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
}).AllowAnonymous();

app.MapControllers();

// SPA fallback: any request that doesn't match an API controller route is handed to Angular's
// client-side router by returning index.html. AllowAnonymous is required because the global
// FallbackPolicy demands a JWT, which the browser won't have on a hard-refresh of /cart etc.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

// Expose Program as a public partial class so WebApplicationFactory<Program>
// can reference it from the test assembly without InternalsVisibleTo.
public partial class Program { }
