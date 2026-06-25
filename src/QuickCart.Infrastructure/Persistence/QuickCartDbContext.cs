using Microsoft.EntityFrameworkCore;
using QuickCart.Domain.Catalog.Entities;
using QuickCart.Domain.Ordering.Aggregates;
using QuickCart.Domain.Shared.Users;

namespace QuickCart.Infrastructure.Persistence;

public sealed class QuickCartDbContext : DbContext
{
    public QuickCartDbContext(DbContextOptions<QuickCartDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.UserId);
            user.Property(u => u.EntraObjectId).IsRequired().HasMaxLength(100);
            user.Property(u => u.Email).IsRequired().HasMaxLength(256);
            user.Property(u => u.DisplayName).HasMaxLength(256);
            user.Property(u => u.PhoneNumber).HasMaxLength(32);
            // One local record per Entra identity.
            user.HasIndex(u => u.EntraObjectId).IsUnique();
            user.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.HasKey(c => c.CategoryId);
            category.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
            category.Property(c => c.Description).HasMaxLength(500);
            // Soft-deleted rows are hidden from every query automatically.
            category.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.HasKey(p => p.ProductId);
            product.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
            product.Property(p => p.Description).HasMaxLength(1000);
            product.Property(p => p.Price).HasPrecision(18, 2);
            product.Property(p => p.ImageUrl).HasMaxLength(2048);
            product.HasIndex(p => p.CategoryId);
            product.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId);
            product.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<Cart>(cart =>
        {
            cart.HasKey(c => c.CartId);
            cart.Property(c => c.UserId);
            // One active cart per user.
            cart.HasIndex(c => c.UserId).IsUnique();
            cart.HasQueryFilter(c => !c.IsDeleted);

            cart.OwnsMany(c => c.Items, item =>
            {
                item.WithOwner();
                item.HasKey(i => i.CartItemId);
                // Key is assigned in the constructor (client-generated Guid). Without this, EF
                // treats it as store-generated and mis-tracks newly added lines on a reloaded
                // aggregate, issuing an UPDATE for a non-existent row → DbUpdateConcurrencyException.
                item.Property(i => i.CartItemId).ValueGeneratedNever();
                item.Property(i => i.ProductId);
                item.Property(i => i.Quantity);
            });
        });

        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.OrderId);
            order.Property(o => o.UserId);
            order.Property(o => o.Status);
            order.Property(o => o.TotalAmount).HasPrecision(18, 2);
            order.HasIndex(o => o.UserId);
            order.HasQueryFilter(o => !o.IsDeleted);

            // Recorded domain events are not persisted.
            order.Ignore(o => o.DomainEvents);

            // OrderItem is owned by the aggregate root.
            order.OwnsMany(o => o.Items, item =>
            {
                item.WithOwner();
                item.HasKey(i => i.OrderItemId);
                item.Property(i => i.OrderItemId).ValueGeneratedNever();
                item.Property(i => i.ProductId);
                item.Property(i => i.UnitPrice).HasPrecision(18, 2);
                item.Property(i => i.Quantity);
                item.Ignore(i => i.LineTotal);
            });
        });
    }
}
