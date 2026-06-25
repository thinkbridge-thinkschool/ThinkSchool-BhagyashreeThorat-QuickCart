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

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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

        // 3. Checkout — should return 201 Created.
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

        // 5. Order should appear in the user's order list.
        var ordersResponse = await _client.GetAsync("/api/v1/orders");
        Assert.Equal(HttpStatusCode.OK, ordersResponse.StatusCode);
        var orders = JsonSerializer.Deserialize<JsonElement[]>(
            await ordersResponse.Content.ReadAsStringAsync(), Json)!;
        Assert.Contains(orders, o => o.GetProperty("orderId").GetString() == orderId);

        // 6. Order should be retrievable by ID.
        var byIdResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);

        // 7. Cancel the order.
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

    private async Task<Guid> GetFirstAvailableProductIdAsync()
    {
        var listResponse = await _client.GetAsync("/api/v1/products");
        var products = JsonSerializer.Deserialize<JsonElement[]>(
            await listResponse.Content.ReadAsStringAsync(), Json)!;
        var available = products.First(p => p.GetProperty("isAvailable").GetBoolean());
        return Guid.Parse(available.GetProperty("productId").GetString()!);
    }
}
