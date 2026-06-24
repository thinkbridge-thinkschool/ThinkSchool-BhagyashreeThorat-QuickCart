using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Carts;
using QuickCart.Domain.Catalog.Entities;
using QuickCart.Infrastructure.Persistence;

namespace QuickCart.Tests;

/// <summary>
/// Reproduces the real "add several different products" flow against a SQLite database, with a
/// fresh DbContext per operation (mirroring separate HTTP requests), to pin down whether the
/// single-item-cart symptom is a backend bug.
/// </summary>
public class CartWorkflowTests
{
    private static QuickCartDbContext NewContext(SqliteConnection conn) =>
        new(new DbContextOptionsBuilder<QuickCartDbContext>().UseSqlite(conn).Options);

    [Fact]
    public async Task AddingThreeDifferentProducts_KeepsAllThreeLines()
    {
        // One in-memory SQLite db shared across contexts by keeping the connection open.
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        using (var ctx = NewContext(conn))
            await ctx.Database.EnsureCreatedAsync();

        Guid rice, chips, juice;
        using (var ctx = NewContext(conn))
        {
            var cat = new Category("Grocery", null, DateTime.UtcNow);
            ctx.Categories.Add(cat);
            var p1 = new Product(cat.CategoryId, "Rice", null, 12.99m, null, 100, DateTime.UtcNow);
            var p2 = new Product(cat.CategoryId, "Chips", null, 1.99m, null, 100, DateTime.UtcNow);
            var p3 = new Product(cat.CategoryId, "Juice", null, 3.20m, null, 100, DateTime.UtcNow);
            ctx.Products.AddRange(p1, p2, p3);
            await ctx.SaveChangesAsync();
            rice = p1.ProductId; chips = p2.ProductId; juice = p3.ProductId;
        }

        var userId = Guid.NewGuid();

        async Task AddAsync(Guid productId)
        {
            using var ctx = NewContext(conn);
            var svc = new CartService(new CartRepository(ctx), new ProductRepository(ctx));
            await svc.AddItemAsync(userId, productId, 1);
        }

        await AddAsync(rice);
        await AddAsync(chips);
        await AddAsync(juice);

        using (var ctx = NewContext(conn))
        {
            var svc = new CartService(new CartRepository(ctx), new ProductRepository(ctx));
            var view = await svc.GetCartAsync(userId);
            Assert.Equal(3, view.Lines.Count);
        }
    }
}
