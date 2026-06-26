using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace QuickCart.Api.HealthChecks;

/// <summary>
/// Writes a structured JSON health report instead of the framework's plain-text default.
///
/// Response shape:
/// {
///   "status": "Healthy",
///   "totalDurationMs": 45.2,
///   "entries": {
///     "database":    { "status": "Healthy",   "durationMs": 23.1, "description": null, "error": null },
///     "service-bus": { "status": "Degraded",  "durationMs": 22.1, "description": "...", "error": "..." }
///   }
/// }
/// </summary>
internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status      = kvp.Value.Status.ToString(),
                    durationMs  = Math.Round(kvp.Value.Duration.TotalMilliseconds, 2),
                    description = kvp.Value.Description,
                    error       = kvp.Value.Exception?.Message,
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
