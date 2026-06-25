using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Catalog;
using QuickCart.Contracts.Catalog;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly CatalogService _catalog;

    public ProductsController(CatalogService catalog) => _catalog = catalog;

    /// <summary>Get products. Optional <paramref name="search"/> and <paramref name="categoryId"/> filters.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
    {
        IReadOnlyList<Product> products =
            categoryId is { } id ? await _catalog.GetProductsByCategoryAsync(id, ct)
            : !string.IsNullOrWhiteSpace(search) ? await _catalog.SearchProductsAsync(search, ct)
            : await _catalog.GetProductsAsync(ct);

        return Ok(products.Select(ToResponse).ToList());
    }

    /// <summary>Get a single product by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _catalog.GetProductAsync(id, ct);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    private static ProductResponse ToResponse(Product p) => new(
        p.ProductId, p.CategoryId, p.ProductName, p.Description,
        p.Price, p.ImageUrl, p.StockQuantity, p.IsAvailable);
}
