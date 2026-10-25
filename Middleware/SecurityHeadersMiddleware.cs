using System.Security.Cryptography;

namespace AutoRent.Middleware;

/// <summary>
/// Middleware для добавления заголовков безопасности
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Защита от MIME-type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Защита от clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Защита от XSS
        context.Response.Headers.Append("X-XSS-Protection", "0");

        // HSTS (HTTP Strict Transport Security) - только если используется HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // Content Security Policy
        // Позволяет скрипты только с того же источника и от Bootstrap CDN
        // Позволяет стили с того же источника и от Bootstrap/Bootstrap-icons CDN
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["CspNonce"] = nonce;
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net; " +
            "font-src 'self' cdn.jsdelivr.net; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self'");

        // Referrer Policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Feature Policy / Permissions Policy
        context.Response.Headers.Append("Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), " +
            "microphone=(), payment=(), usb=()");

        await _next(context);
    }
}
