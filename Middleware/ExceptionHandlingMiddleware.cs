using System.Net;
using System.Text.Json;

namespace AutoRent.Middleware;

/// <summary>
/// Middleware для обработки необработанных исключений
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden, "Доступ запрещен");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound, "Ресурс не найден");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Неверный аргумент");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Недопустимая операция");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError,
                "Внутренняя ошибка сервера");
        }
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var isDevelopment = context.RequestServices
            .GetService<IHostEnvironment>()?.IsDevelopment() ?? false;

        object response = isDevelopment
            ? (object)new { error = message, details = exception.Message, stackTrace = exception.StackTrace }
            : new { error = message };

        return context.Response.WriteAsJsonAsync(response);
    }
}
