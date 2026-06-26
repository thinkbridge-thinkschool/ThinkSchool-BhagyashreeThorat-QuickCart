using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickCart.IntegrationTests;

/// <summary>
/// Tests the complete checkout journey end-to-end through the real API pipeline.
/// Uses a dedicated ApiFactory so the database is isolated from CartApiTests.
/// </summary>
public class OrdersApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public OrdersApiTests(ApiFactory factory) => _client = factory.CreateClient();

    // ── Checkout (POST /orders) ───────────────────────────────────────────

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/orders", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /orders — paged response ──────────────────────────────────────

    [Fact]
    public async Task GetOrders_ReturnsPagedEnvelope()
    {
        var body = await GetPagedOrdersAsync("/api/v1/orders");

        Assert.True(body.TryGetProperty("items",       out _));
        Assert.True(body.TryGetProperty("page",        out _));
        Assert.True(body.TryGetProperty("pageSize",    out _));
        Assert.True(body.TryGetProperty("totalItems",  out _));
        Assert.True(body.TryGetProperty("totalPages",  out _));
        Assert.True(body.TryGetProperty("hasPrevious", out _));
        Assert.True(body.TryGetProperty("hasNext",     out _));
    }

    [Fact]
    public async Task GetOrders_PageZero_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/orders?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_PageSizeOverMax_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/orders?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Full checkout journey ─────────────────────────────────────────────

    [Fact]
    public async Task FullCheckoutJourney_CreatesOrderAndClearsCart()
    {
        // 1. Add a product to the cart.
        var productId = await GetFirstAvailableProductIdAsync();
        var addResponse = await _client.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId,
            quantity = 2
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        // 2. Verify the cart has the item.
        var cartResponse = await _client.GetAsync("/api/v1/cart");
        var cart = JsonSerializer.Deserialize<JsonElement>(
            await cartResponse.Content.ReadAsStringAsync(), Json);
        Assert.True(cart.GetProperty("items").GetArrayLength() > 0);

        // 3. Checkout — should return 201 Created with a single OrderResponse.
        var orderResponse = await _client.PostAsync("/api/v1/orders", null);
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = JsonSerializer.Deserialize<JsonElement>(
            await orderResponse.Content.ReadAsStringAsync(), Json);
        var orderId = order.GetProperty("orderId").GetString()!;
        Assert.Equal("Confirmed", order.GetProperty("status").GetString());
        Assert.True(order.GetProperty("totalAmount").GetDecimal() > 0);

        // 4. Cart should be empty after checkout.
        var emptyCartResponse = await _client.GetAsync("/api/v1/cart");
        var emptyCart = JsonSerializer.Deserialize<JsonElement>(
            await emptyCartResponse.Content.ReadAsStringAsync(), Json);
        Assert.Equal(0, emptyCart.GetProperty("items").GetArrayLength());

        // 5. Order appears in the paged order list.
        var pagedOrders = await GetPagedOrdersAsync("/api/v1/orders");
        var orderItems = pagedOrders.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(orderItems, o => o.GetProperty("orderId").GetString() == orderId);

        // 6. Total count and page metadata are consistent.
        Assert.True(pagedOrders.GetProperty("totalItems").GetInt32() >= 1);
        Assert.Equal(1, pagedOrders.GetProperty("page").GetInt32());

        // 7. Order retrievable by ID.
        var byIdResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);

        // 8. Cancel the order.
        var cancelResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = JsonSerializer.Deserialize<JsonElement>(
            await cancelResponse.Content.ReadAsStringAsync(), Json);
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetOrderById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<JsonElement> GetPagedOrdersAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
    }

    private async Task<Guid> GetFirstAvailableProductIdAsync()
    {
        var response = await _client.GetAsync("/api/v1/products?pageSize=100");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);
        var available = body.GetProperty("items").EnumerateArray()
            .First(p => p.GetProperty("isAvailable").GetBoolean());
        return Guid.Parse(available.GetProperty("productId").GetString()!);
    }
}
