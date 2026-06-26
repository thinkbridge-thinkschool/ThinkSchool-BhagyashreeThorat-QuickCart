using System.Net;
using System.Text.Json;

namespace QuickCart.IntegrationTests;

/// <summary>
/// Verifies both health check endpoints against the in-process test server.
/// The test environment uses SQLite in-memory (always healthy) and has no
/// Service Bus configured (so the "service-bus" check is simply absent).
/// </summary>
public class HealthCheckApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HealthCheckApiTests(ApiFactory factory) => _client = factory.CreateClient();

    // ── /health (liveness) ────────────────────────────────────────────────

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Liveness_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Liveness_HasNoEntries()
    {
        // Liveness runs no checks (Predicate = _ => false) — entries must be empty.
        var response = await _client.GetAsync("/health");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        Assert.Empty(body.GetProperty("entries").EnumerateObject());
    }

    [Fact]
    public async Task Liveness_ContentTypeIsJson()
    {
        var response = await _client.GetAsync("/health");
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ── /health/ready (readiness) ─────────────────────────────────────────

    [Fact]
    public async Task Readiness_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health/ready");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_IncludesDatabaseEntry()
    {
        var response = await _client.GetAsync("/health/ready");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        var entries = body.GetProperty("entries");
        Assert.True(entries.TryGetProperty("database", out var db),
            "Expected a 'database' entry in readiness response.");
        Assert.Equal("Healthy", db.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Readiness_DatabaseEntryHasDurationMs()
    {
        var response = await _client.GetAsync("/health/ready");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        var db = body.GetProperty("entries").GetProperty("database");
        Assert.True(db.GetProperty("durationMs").GetDouble() >= 0);
    }

    [Fact]
    public async Task Readiness_NoServiceBusEntryInTestEnvironment()
    {
        // Service Bus is not configured in the test factory (no namespace set),
        // so the check should not appear in the response.
        var response = await _client.GetAsync("/health/ready");
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(), Json);

        var entries = body.GetProperty("entries");
        Assert.False(entries.TryGetProperty("service-bus", out _),
            "service-bus check should be absent when namespace is not configured.");
    }

    // ── Anonymous access ──────────────────────────────────────────────────

    [Fact]
    public async Task HealthEndpoints_AreAccessibleWithoutAuth()
    {
        // These must never return 401/403 regardless of the auth FallbackPolicy.
        var liveness   = await _client.GetAsync("/health");
        var readiness  = await _client.GetAsync("/health/ready");

        Assert.NotEqual(HttpStatusCode.Unauthorized, liveness.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden,    liveness.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, readiness.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden,    readiness.StatusCode);
    }
}
