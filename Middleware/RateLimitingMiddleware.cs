using System.Collections.Concurrent;

namespace AutoRent.Middleware;

/// <summary>
/// Middleware для защиты от brute-force атак
/// Ограничивает количество запросов с одного IP-адреса
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // IP -> List of request timestamps
    private static readonly ConcurrentDictionary<string, List<DateTime>> RequestHistory =
        new();
    private static long _lastCleanupTicks;

    private const int MaxRequestsPerMinute = 100;
    private const int TimeWindowSeconds = 60;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        // Skip rate limiting for localhost
        if (clientIp == "127.0.0.1" || clientIp == "::1")
        {
            await _next(context);
            return;
        }

        // Get or create request history for this IP
        var requests = RequestHistory.GetOrAdd(clientIp, _ => new List<DateTime>());

        bool isLimitExceeded;

        lock (requests)
        {
            // Remove requests older than the time window
            requests.RemoveAll(d => (now - d).TotalSeconds > TimeWindowSeconds);

            // Check if limit is exceeded
            isLimitExceeded = requests.Count >= MaxRequestsPerMinute;

            if (!isLimitExceeded)
            {
                // Add current request only if limit not exceeded
                requests.Add(now);
            }
        }

        if (isLimitExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for IP {ClientIp}. Requests: {RequestCount} in {TimeWindowSeconds}s",
                clientIp, MaxRequestsPerMinute, TimeWindowSeconds);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";

            var response = new { error = "Слишком много запросов. Пожалуйста, попробуйте позже." };
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        CleanupExpiredClients(now);
        await _next(context);
    }

    private static void CleanupExpiredClients(DateTime now)
    {
        var previous = Interlocked.Read(ref _lastCleanupTicks);
        if (now.Ticks - previous < TimeSpan.FromMinutes(1).Ticks ||
            Interlocked.CompareExchange(ref _lastCleanupTicks, now.Ticks, previous) != previous)
            return;

        foreach (var entry in RequestHistory)
        {
            lock (entry.Value)
            {
                entry.Value.RemoveAll(timestamp => (now - timestamp).TotalSeconds > TimeWindowSeconds);
                if (entry.Value.Count == 0)
                    RequestHistory.TryRemove(entry.Key, out _);
            }
        }
    }

    /// <summary>
    /// Получить IP-адрес клиента с учетом прокси
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header (for proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var forwardedIp = forwarded.ToString().Split(',').First().Trim();
            return forwardedIp;
        }

        // Check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString();
        }

        // Use RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
