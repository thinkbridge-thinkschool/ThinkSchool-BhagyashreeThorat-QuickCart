using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using QuickCart.Application.Catalog;
using QuickCart.Contracts.Catalog;
using QuickCart.Contracts.Common;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly CatalogService _catalog;

    public ProductsController(CatalogService catalog) => _catalog = catalog;

    /// <summary>
    /// Get a paginated page of products.
    /// Optional <c>search</c> and <c>categoryId</c> filters are applied before paging.
    /// </summary>
    /// <param name="search">Optional free-text search over product name and description.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="page">1-based page number (default 1, must be >= 1).</param>
    /// <param name="pageSize">Items per page (default 10, range 1–100).</param>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1)
            return Problem(
                detail: "'page' must be greater than or equal to 1.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination parameters");

        if (pageSize < 1 || pageSize > 200)
            return Problem(
                detail: "'pageSize' must be between 1 and 200.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination parameters");

        var (items, total) = await _catalog.GetProductsPagedAsync(
            page, pageSize, search, categoryId, ct);

        return Ok(PagedResponse<ProductResponse>.From(
            items.Select(ToResponse).ToList(),
            total, page, pageSize));
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
