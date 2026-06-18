using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuickCart.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by the EF Core tools (migrations). At runtime the context is
/// configured through DI in <see cref="DependencyInjection"/>; that path falls back to the
/// in-memory provider locally, which can't scaffold relational migrations — so the tools use
/// this SqlServer-configured context instead. The connection string is never connected to here.
/// </summary>
public sealed class QuickCartDbContextFactory : IDesignTimeDbContextFactory<QuickCartDbContext>
{
    public QuickCartDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<QuickCartDbContext>()
            .UseSqlServer("Server=design-time;Database=quickcart;Trusted_Connection=True;")
            .Options;

        return new QuickCartDbContext(options);
    }
}
