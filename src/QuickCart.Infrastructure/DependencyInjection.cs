using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickCart.Application.Abstractions;
using QuickCart.Application.Orders;
using QuickCart.Domain.Ordering.Events;
using QuickCart.Infrastructure.HealthChecks;
using QuickCart.Infrastructure.Messaging;
using QuickCart.Infrastructure.Persistence;

namespace QuickCart.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers persistence and messaging for the Ordering context.
    /// Both the SQL and Service Bus paths authenticate with the App Service managed
    /// identity — there are no connection-string secrets anywhere.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<QuickCartDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Local dev: no Azure SQL configured → a SQLite file. Unlike the in-memory
                // provider, this persists carts and orders across API restarts (the in-memory
                // store reset on every boot, which wiped the cart between adds).
                var sqlitePath = configuration["Sqlite:DataSource"] ?? "quickcart.db";
                options.UseSqlite($"Data Source={sqlitePath}");
            }
            else
            {
                // Cloud: the connection string carries "Authentication=Active Directory Default".
                // Microsoft.Data.SqlClient acquires an Entra token via the managed identity —
                // no User ID / Password, so nothing secret lives in the connection string.
                options.UseSqlServer(connectionString);
            }
        });

        // Service Bus over Managed Identity. We register a client only when a namespace is
        // configured (cloud). DefaultAzureCredential uses the App Service MI in Azure and
        // the developer's az-login locally — never a SAS key.
        var serviceBusNamespace = configuration["ServiceBus:FullyQualifiedNamespace"];
        var queueName = configuration["ServiceBus:QueueName"] ?? "order-events";
        if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            services.AddSingleton(_ => new ServiceBusClient(
                serviceBusNamespace,
                new DefaultAzureCredential()));

            // Cloud: publish ordering events over Service Bus (Managed Identity).
            services.AddSingleton<IOrderEventPublisher>(sp =>
                new ServiceBusOrderEventPublisher(sp.GetRequiredService<ServiceBusClient>(), queueName));
        }
        else
        {
            // Local dev / tests: no broker, so order creation just skips the publish.
            services.AddSingleton<IOrderEventPublisher, NullOrderEventPublisher>();
        }

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICartRepository, CartRepository>();

        // Domain-event dispatch: recorded events → handlers → integration events.
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
        return services;
    }

    /// <summary>
    /// Registers infrastructure-level health checks.
    /// Called separately from AddInfrastructure so the caller can chain additional
    /// application-level checks before finalising the builder.
    ///
    /// Checks registered:
    ///   "database"    – EF Core can open a connection (SQLite or SQL Server). Tag: ready.
    ///   "service-bus" – Service Bus queue is reachable via admin API. Tag: ready.
    ///                   Only registered when ServiceBus:FullyQualifiedNamespace is configured.
    ///                   Failure maps to Degraded (not Unhealthy) because the API can still
    ///                   serve requests without Service Bus; order events are simply not published.
    /// </summary>
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        // Database check works for both SQLite (dev) and SQL Server (cloud)
        // because AddDbContextCheck opens a connection through the same provider
        // that DependencyInjection.cs registered.
        builder.AddDbContextCheck<QuickCartDbContext>(
            name: "database",
            tags: ["ready"]);

        var serviceBusNamespace = configuration["ServiceBus:FullyQualifiedNamespace"];
        var queueName = configuration["ServiceBus:QueueName"] ?? "order-events";

        if (!string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            // Register the administration client used only by the health check.
            // The messaging ServiceBusClient registered in AddInfrastructure is a
            // data-plane client; this is a separate management-plane client.
            builder.Services.AddSingleton(
                new ServiceBusAdministrationClient(serviceBusNamespace, new DefaultAzureCredential()));

            builder.Add(new HealthCheckRegistration(
                name: "service-bus",
                factory: sp => new ServiceBusHealthCheck(
                    sp.GetRequiredService<ServiceBusAdministrationClient>(),
                    queueName),
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"]));
        }

        return builder;
    }
}
