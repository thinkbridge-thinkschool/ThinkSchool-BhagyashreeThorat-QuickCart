using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickCart.Application.Abstractions;
using QuickCart.Infrastructure.Persistence;

namespace QuickCart.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers persistence for the Ordering context. Swap the in-memory store
    /// for SQL Server (already cached) by changing the provider here.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<QuickCartDbContext>(options =>
            options.UseInMemoryDatabase("quickcart"));

        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
