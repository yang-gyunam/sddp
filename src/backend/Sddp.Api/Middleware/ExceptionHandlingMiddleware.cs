using Sddp.Diagnostics;
using System.Net;
using System.Text.Json;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;

namespace Sddp.Api.Middleware;

/// <summary>
/// Global exception handling middleware for the public runtime.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (HttpStatusCode statusCode, object response) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                ApiResponse<object>.Fail("NOT_FOUND", notFound.Message)),

            ValidationException validation => (
                HttpStatusCode.BadRequest,
                CreateValidationResponse(validation)),

            UnauthorizedException unauthorized => (
                HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail("UNAUTHORIZED", unauthorized.Message)),

            ForbiddenAccessException forbidden => (
                HttpStatusCode.Forbidden,
                ApiResponse<object>.Fail("FORBIDDEN", forbidden.Message)),

            InvalidOperationException invalidOp => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("OPERATION_ERROR", invalidOp.Message)),

            ConflictException conflict => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Fail("CONFLICT", conflict.Message)),

            SpecLockedException locked => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Fail("SPEC_LOCKED", locked.Message)),

            KnowledgePageEditConflictException knowledgeConflict => (
                HttpStatusCode.Conflict,
                (object)new
                {
                    success = false,
                    errorCode = knowledgeConflict.ErrorCode,
                    errorMessage = knowledgeConflict.Message,
                    details = new
                    {
                        expectedRevision = knowledgeConflict.ExpectedRevision,
                        currentRevision = knowledgeConflict.CurrentRevision
                    }
                }),

            DiagramSchemaConflictException schemaConflict => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Fail(schemaConflict.ErrorCode, schemaConflict.Message)),

            SddpException { ErrorCode: "NOT_ALLOWED" } notAllowed => (
                HttpStatusCode.Forbidden,
                ApiResponse<object>.Fail(notAllowed.ErrorCode, notAllowed.Message)),

            SddpException sddp => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail(sddp.ErrorCode, sddp.Message)),

            OperationCanceledException => (
                HttpStatusCode.RequestTimeout,
                ApiResponse<object>.Fail("REQUEST_CANCELLED", "Request was cancelled")),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateInternalErrorResponse(exception))
        };

        LogException(exception, context, statusCode);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private ApiResponse<object> CreateValidationResponse(ValidationException exception)
    {
        var errorMessage = exception.Errors.Count > 0
            ? string.Join("; ", exception.Errors.SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}")))
            : exception.Message;

        return ApiResponse<object>.Fail("VALIDATION_ERROR", errorMessage);
    }

    private ApiResponse<object> CreateInternalErrorResponse(Exception exception)
    {
        var message = _environment.IsDevelopment()
            ? $"{exception.Message} | StackTrace: {exception.StackTrace}"
            : "An unexpected error occurred";

        return ApiResponse<object>.Fail("INTERNAL_ERROR", message);
    }

    private void LogException(Exception exception, HttpContext context, HttpStatusCode statusCode)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        _logger.Log(statusCode == HttpStatusCode.InternalServerError ? LogLevel.Error : LogLevel.Warning,
            "Request failed. CorrelationId {CorrelationId}, HTTP {StatusCode}, path {Path}, {ErrorType}: {Reason} {StackTrace}",
            correlationId, (int)statusCode, DiagnosticMask.Url(context.Request.Path),
            exception.GetType().Name, DiagnosticMask.Text(exception.Message), DiagnosticMask.Text(exception.StackTrace, 16000));
    }
}

/// <summary>
/// Middleware registration extensions.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
