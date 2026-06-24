using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickCart.Application.Abstractions;
using QuickCart.Application.Orders;
using QuickCart.Domain.Ordering.Events;
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
}
