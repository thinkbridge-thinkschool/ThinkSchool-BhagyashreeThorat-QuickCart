using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using QuickCart.Application.Orders;
using QuickCart.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

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
