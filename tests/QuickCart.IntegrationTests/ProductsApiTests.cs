using System.Net;
using System.Text.Json;

namespace QuickCart.IntegrationTests;

public class ProductsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProductsApiTests(ApiFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetProducts_ReturnsOkWithSeededProducts()
    {
        var response = await _client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<JsonElement[]>(body, Json)!;
        Assert.True(products.Length > 0, "Seeded products should be returned.");
    }

    [Fact]
    public async Task GetProducts_SearchByName_ReturnsMatchingProducts()
    {
        var response = await _client.GetAsync("/api/v1/products?search=rice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = JsonSerializer.Deserialize<JsonElement[]>(
            await response.Content.ReadAsStringAsync(), Json)!;

        Assert.Contains(products, p =>
            p.GetProperty("productName").GetString()!
                .Contains("Rice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProduct()
    {
        // First get any product ID from the catalog.
        var listResponse = await _client.GetAsync("/api/v1/products");
        var products = JsonSerializer.Deserialize<JsonElement[]>(
            await listResponse.Content.ReadAsStringAsync(), Json)!;
        var id = products[0].GetProperty("productId").GetString();

        var response = await _client.GetAsync($"/api/v1/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        Assert.Equal(id, product.GetProperty("productId").GetString());
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
}
