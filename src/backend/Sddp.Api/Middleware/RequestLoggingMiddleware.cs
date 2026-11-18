using Sddp.Diagnostics;
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
        var correlationId = Guid.TryParse(context.Request.Headers[CorrelationIdHeader].FirstOrDefault(), out var supplied)
            ? supplied.ToString("N") : Guid.NewGuid().ToString("N");
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";

        var path = DiagnosticMask.Url(context.Request.Path) ?? route;
        var method = context.Request.Method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS"
            ? context.Request.Method : "OTHER";

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
        using (LogContext.PushProperty("RequestPath", path))
        using (LogContext.PushProperty("Route", route))
        using (LogContext.PushProperty("RequestMethod", method))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Log request start.
                _logger.LogInformation(
                    "Request started: {Method} {Path}",
                    method,
                    path);

                await _next(context);

                stopwatch.Stop();

                // Log request completion.
                _logger.LogInformation(
                    "Request completed: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
                    method,
                    path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "Request failed: {Method} {Path} after {ElapsedMs}ms",
                    method,
                    path,
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
