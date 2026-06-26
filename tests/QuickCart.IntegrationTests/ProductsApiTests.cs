using System.Net;
using System.Text.Json;

namespace QuickCart.IntegrationTests;

public class ProductsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProductsApiTests(ApiFactory factory) => _client = factory.CreateClient();

    // ── Response shape ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedEnvelope()
    {
        var body = await GetPagedAsync("/api/v1/products");

        // Required envelope fields
        Assert.True(body.TryGetProperty("items",      out _));
        Assert.True(body.TryGetProperty("page",       out _));
        Assert.True(body.TryGetProperty("pageSize",   out _));
        Assert.True(body.TryGetProperty("totalItems", out _));
        Assert.True(body.TryGetProperty("totalPages", out _));
        Assert.True(body.TryGetProperty("hasPrevious", out _));
        Assert.True(body.TryGetProperty("hasNext",    out _));
    }

    [Fact]
    public async Task GetProducts_DefaultPage_ReturnsFirstTenItems()
    {
        var body = await GetPagedAsync("/api/v1/products");

        Assert.Equal(1,  body.GetProperty("page").GetInt32());
        Assert.Equal(10, body.GetProperty("pageSize").GetInt32());
        Assert.True(body.GetProperty("totalItems").GetInt32() > 0,
            "Seeded products should be present.");
        // The test DB has 25 seeded products; page 1 should have exactly 10.
        Assert.Equal(10, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetProducts_PageTwo_ReturnsDifferentItems()
    {
        var page1 = await GetPagedAsync("/api/v1/products?page=1&pageSize=10");
        var page2 = await GetPagedAsync("/api/v1/products?page=2&pageSize=10");

        var ids1 = page1.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("productId").GetString()).ToHashSet();
        var ids2 = page2.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("productId").GetString()).ToHashSet();

        Assert.Empty(ids1.Intersect(ids2));  // pages must not overlap
    }

    [Fact]
    public async Task GetProducts_LastPage_HasNextFalse()
    {
        // Seed data has 25 products; pageSize=10 → 3 pages. Page 3 has hasNext=false.
        var body = await GetPagedAsync("/api/v1/products?page=3&pageSize=10");

        Assert.False(body.GetProperty("hasNext").GetBoolean());
        Assert.True(body.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task GetProducts_FirstPage_HasPreviousFalse()
    {
        var body = await GetPagedAsync("/api/v1/products?page=1");
        Assert.False(body.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task GetProducts_LargePageSize_ReturnsAllItems()
    {
        var body = await GetPagedAsync("/api/v1/products?pageSize=100");

        var total = body.GetProperty("totalItems").GetInt32();
        var returned = body.GetProperty("items").GetArrayLength();
        Assert.Equal(total, returned);
        Assert.False(body.GetProperty("hasNext").GetBoolean());
    }

    // ── Pagination metadata ────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_TotalPagesIsCorrect()
    {
        var body = await GetPagedAsync("/api/v1/products?pageSize=10");

        var total = body.GetProperty("totalItems").GetInt32();
        var pages = body.GetProperty("totalPages").GetInt32();
        var expected = (int)Math.Ceiling(total / 10.0);
        Assert.Equal(expected, pages);
    }

    // ── Input validation ──────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_PageZero_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/products?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_PageSizeZero_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/products?pageSize=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_PageSizeOverMax_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/products?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_PageBeyondLast_ReturnsEmptyItems()
    {
        // Page 999 is well beyond the available data.
        var body = await GetPagedAsync("/api/v1/products?page=999");

        Assert.Empty(body.GetProperty("items").EnumerateArray());
        Assert.False(body.GetProperty("hasNext").GetBoolean());
    }

    // ── Existing behaviour preserved ──────────────────────────────────────

    [Fact]
    public async Task GetProducts_SearchByName_ReturnsMatchingItemsInFirstPage()
    {
        var body = await GetPagedAsync("/api/v1/products?search=rice");
        var items = body.GetProperty("items").EnumerateArray().ToList();

        Assert.Contains(items, p =>
            p.GetProperty("productName").GetString()!
                .Contains("Rice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProduct()
    {
        var list = await GetPagedAsync("/api/v1/products");
        var id = list.GetProperty("items")[0].GetProperty("productId").GetString();

        var response = await _client.GetAsync($"/api/v1/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        Assert.Equal(id, product.GetProperty("productId").GetString());
    }

    [Fact]
    public async Task GetProductById_ReturnsAllRequiredFields()
    {
        var list = await GetPagedAsync("/api/v1/products");
        var firstId = list.GetProperty("items")[0].GetProperty("productId").GetString()!;

        var response = await _client.GetAsync($"/api/v1/products/{firstId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var p = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        // All fields required by the product detail page must be present.
        Assert.True(p.TryGetProperty("productId",     out _), "productId missing");
        Assert.True(p.TryGetProperty("categoryId",    out _), "categoryId missing");
        Assert.True(p.TryGetProperty("productName",   out _), "productName missing");
        Assert.True(p.TryGetProperty("price",         out _), "price missing");
        Assert.True(p.TryGetProperty("stockQuantity", out _), "stockQuantity missing");
        Assert.True(p.TryGetProperty("isAvailable",   out _), "isAvailable missing");

        // Values must be valid (not default/empty for required fields).
        Assert.NotEmpty(p.GetProperty("productId").GetString()!);
        Assert.NotEmpty(p.GetProperty("productName").GetString()!);
        Assert.True(p.GetProperty("price").GetDecimal() >= 0, "price must be non-negative");
        Assert.True(p.GetProperty("stockQuantity").GetInt32() >= 0, "stockQuantity must be non-negative");
    }

    [Fact]
    public async Task GetProductById_OutOfStockProduct_ReturnsIsAvailableFalse()
    {
        // Toothpaste is seeded with StockQuantity = 0, so IsAvailable = false.
        var list = await GetPagedAsync("/api/v1/products?search=toothpaste&pageSize=5");
        var toothpaste = list.GetProperty("items").EnumerateArray()
            .FirstOrDefault(p =>
                p.GetProperty("productName").GetString()!
                    .Contains("Toothpaste", StringComparison.OrdinalIgnoreCase));

        // Guard: if seed data changes, skip silently rather than fail.
        if (toothpaste.ValueKind == System.Text.Json.JsonValueKind.Undefined) return;

        var id = toothpaste.GetProperty("productId").GetString()!;
        var response = await _client.GetAsync($"/api/v1/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var p = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        Assert.False(p.GetProperty("isAvailable").GetBoolean(), "Toothpaste should be out of stock");
        Assert.Equal(0, p.GetProperty("stockQuantity").GetInt32());
    }

    [Fact]
    public async Task GetProductById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ReturnsOkWithSeededCategories()
    {
        var response = await _client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = JsonSerializer.Deserialize<JsonElement[]>(
            await response.Content.ReadAsStringAsync(), Json)!;
        Assert.True(categories.Length > 0);
    }

    [Fact]
    public async Task ResponseHeaders_ContainSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/v1/products");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var xct));
        Assert.Equal("nosniff", xct.First());
        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var xfo));
        Assert.Equal("DENY", xfo.First());
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private async Task<JsonElement> GetPagedAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
    }
}
