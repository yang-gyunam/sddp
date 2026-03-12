using System.Diagnostics;
using Serilog.Context;

namespace Sddp.Api.Middleware;

/// <summary>
/// Request logging middleware.
/// Generates correlation IDs, logs requests and responses, and records timing.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private const string CorrelationIdHeader = "X-Correlation-Id";

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a correlation ID or reuse the one from the request header.
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..16];

        // Store it in the request context.
        context.Items["CorrelationId"] = correlationId;

        // Add it to the response headers.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push it into the Serilog log context.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Log request start.
                _logger.LogInformation(
                    "Request started: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await _next(context);

                stopwatch.Stop();

                // Log request completion.
                _logger.LogInformation(
                    "Request completed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "Request failed: {Method} {Path} after {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}

/// <summary>
/// Middleware registration extensions.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
