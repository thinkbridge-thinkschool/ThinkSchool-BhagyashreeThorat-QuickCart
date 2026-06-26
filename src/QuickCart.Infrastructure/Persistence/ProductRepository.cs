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

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? categoryId = null,
        CancellationToken ct = default)
    {
        // Build the base query with optional filters applied before paging.
        // EF Core translates this to a single parameterised WHERE clause.
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowered = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(lowered) ||
                (p.Description != null && p.Description.ToLower().Contains(lowered)));
        }

        // Always order before paging so results are stable across pages.
        var ordered = query.OrderBy(p => p.ProductName);

        // Two separate queries: COUNT(*) then SELECT with Skip/Take.
        // This is the standard pattern — no rows outside the window are hydrated.
        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
