using Microsoft.EntityFrameworkCore;
using QuickCart.Domain.Aggregates;

namespace QuickCart.Infrastructure.Persistence;

public sealed class QuickCartDbContext : DbContext
{
    public QuickCartDbContext(DbContextOptions<QuickCartDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id);
            order.Property(o => o.CustomerId);
            order.Property(o => o.Status);
            order.Property(o => o.CreatedAtUtc);

            // Computed / behavioural members are not persisted.
            order.Ignore(o => o.Total);
            order.Ignore(o => o.DomainEvents);

            // OrderLine is owned by the aggregate root (no independent identity).
            order.OwnsMany(o => o.Lines, line =>
            {
                line.WithOwner();
                line.Property<int>("Id");
                line.HasKey("Id");
                line.Property(l => l.ProductId);
                line.Property(l => l.ProductName);
                line.Property(l => l.UnitPrice);
                line.Property(l => l.Quantity);
                line.Ignore(l => l.LineTotal);
            });
        });
    }
}
