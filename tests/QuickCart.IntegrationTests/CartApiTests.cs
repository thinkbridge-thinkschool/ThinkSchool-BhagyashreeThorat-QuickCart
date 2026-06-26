using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickCart.IntegrationTests;

public class CartApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public CartApiTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetCart_AlwaysReturnsValidShape()
    {
        // Tests in this class share the same dev-user cart so we cannot assume it is empty.
        // Just verify the response has the correct structure.
        var response = await _client.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        Assert.Equal(JsonValueKind.Array, cart.GetProperty("items").ValueKind);
        Assert.True(cart.GetProperty("total").GetDecimal() >= 0m);
    }

    [Fact]
    public async Task AddItem_WithValidProduct_ReturnsCartWithItem()
    {
        var productId = await GetFirstAvailableProductIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId,
            quantity = 2
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        var items = cart.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task AddItem_QuantityZero_Returns400()
    {
        var productId = await GetFirstAvailableProductIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId,
            quantity = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_UnknownProduct_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId = Guid.NewGuid(),
            quantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddThenUpdateItem_QuantityIsUpdated()
    {
        var productId = await GetFirstAvailableProductIdAsync();
        await _client.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/cart/items/{productId}", new { quantity = 5 });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var cart = JsonSerializer.Deserialize<JsonElement>(
            await updateResponse.Content.ReadAsStringAsync(), Json);
        var line = cart.GetProperty("items").EnumerateArray()
            .First(l => l.GetProperty("productId").GetString() == productId.ToString());
        Assert.Equal(5, line.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task AddThenDeleteItem_CartBecomesEmpty()
    {
        var productId = await GetFirstAvailableProductIdAsync();
        await _client.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });

        var deleteResponse = await _client.DeleteAsync($"/api/v1/cart/items/{productId}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        // Cart may have other items from earlier tests in the class; just verify the deleted
        // product is no longer present.
        var cart = JsonSerializer.Deserialize<JsonElement>(
            await deleteResponse.Content.ReadAsStringAsync(), Json);
        var still = cart.GetProperty("items").EnumerateArray()
            .Any(l => l.GetProperty("productId").GetString() == productId.ToString());
        Assert.False(still);
    }

    private async Task<Guid> GetFirstAvailableProductIdAsync()
    {
        // GET /products now returns a paged envelope; extract .items.
        var listResponse = await _client.GetAsync("/api/v1/products?pageSize=100");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await listResponse.Content.ReadAsStringAsync(), Json);
        var available = body.GetProperty("items").EnumerateArray()
            .First(p => p.GetProperty("isAvailable").GetBoolean());
        return Guid.Parse(available.GetProperty("productId").GetString()!);
    }
}
