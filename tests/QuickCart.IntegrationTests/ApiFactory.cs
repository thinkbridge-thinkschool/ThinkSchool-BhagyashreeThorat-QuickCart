using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickCart.Infrastructure.Persistence;

namespace QuickCart.IntegrationTests;

/// <summary>
/// Hosts the QuickCart API in-process with an isolated SQLite in-memory database.
/// Each test class gets its own factory via IClassFixture, so database state never
/// leaks between test classes. The CatalogSeeder (called from Program.cs on startup)
/// creates the schema and seeds sample products automatically.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Keep the connection open for the lifetime of the fixture so all DbContext
    // instances within the same in-memory database see the same data.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production DbContextOptions registration added by
            // DependencyInjection.cs and replace it with our in-memory SQLite connection.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuickCartDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<QuickCartDbContext>(options =>
                options.UseSqlite(_connection));
        });

        // Override to "Testing" so any environment-specific paths are avoided.
        builder.UseEnvironment("Testing");
    }

    // Open the connection first; the host builds lazily on the first CreateClient()
    // call, which runs Program.cs including CatalogSeeder (EnsureCreated + seed data).
    public Task InitializeAsync() => _connection.OpenAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
