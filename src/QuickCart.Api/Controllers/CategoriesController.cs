using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Catalog;
using QuickCart.Contracts.Catalog;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CatalogService _catalog;

    public CategoriesController(CatalogService catalog) => _catalog = catalog;

    /// <summary>Get all categories.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken ct)
    {
        var categories = await _catalog.GetCategoriesAsync(ct);
        return Ok(categories
            .Select(c => new CategoryResponse(c.CategoryId, c.CategoryName, c.Description))
            .ToList());
    }
}
