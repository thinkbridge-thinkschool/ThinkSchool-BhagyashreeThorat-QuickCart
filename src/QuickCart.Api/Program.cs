using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpenTelemetry.Resources;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CartService>();

// CORS for the Angular dev server (local dev only). The cloud frontend is served same-origin
// or configured separately, so this policy is applied only outside production below.
const string DevCorsPolicy = "AngularDev";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

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
