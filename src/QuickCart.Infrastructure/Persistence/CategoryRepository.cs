using Microsoft.EntityFrameworkCore;
using QuickCart.Application.Abstractions;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Infrastructure.Persistence;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly QuickCartDbContext _db;

    public CategoryRepository(QuickCartDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Categories.AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

    public Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken ct = default) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
}
