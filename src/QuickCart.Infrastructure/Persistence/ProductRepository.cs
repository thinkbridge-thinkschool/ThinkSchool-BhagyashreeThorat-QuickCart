using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly QuickCartDbContext _db;

    public ProductRepository(QuickCartDbContext db) => _db = db;

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Products.AsNoTracking()
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken ct = default) =>
        _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId, ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken ct = default)
    {
        // ToLower().Contains() translates on both SQL Server (LOWER + LIKE) and the InMemory
        // provider, so search behaves the same in the cloud and in local dev.
        var lowered = term.ToLowerInvariant();
        return await _db.Products.AsNoTracking()
            .Where(p => p.ProductName.ToLower().Contains(lowered)
                     || (p.Description != null && p.Description.ToLower().Contains(lowered)))
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await _db.Products.AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        // Tracked (no AsNoTracking): checkout may reduce stock on the returned products.
        var ids = productIds.Distinct().ToList();
        return await _db.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
