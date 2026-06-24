using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QuickCart.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by all EF Core tools (migrations add, database update, etc.).
/// At runtime the context is configured through DI. The factory reads the real connection
/// string from appsettings.Development.json so `database update` can connect to SQL Server.
/// </summary>
public sealed class QuickCartDbContextFactory : IDesignTimeDbContextFactory<QuickCartDbContext>
{
    public QuickCartDbContext CreateDbContext(string[] args)
    {
        // Walk up from the Infrastructure project to find the API's appsettings files.
        var apiDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,        // …/bin/Debug/net10.0/
            "..", "..", "..", "..",           // back to src/
            "QuickCart.Api"));

        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<QuickCartDbContext>()
            .UseSqlServer(
                !string.IsNullOrWhiteSpace(connectionString)
                    ? connectionString
                    // Hard fallback for CI / environments without appsettings.Development.json
                    : "Server=.\\SQLEXPRESS;Database=quickcart;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new QuickCartDbContext(options);
    }
}
