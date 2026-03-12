using System.Security.Claims;
using System.Text.Json;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Services;

namespace Sddp.Api.Middleware;

/// <summary>
/// Tenant context middleware.
/// Extracts tenant, project, and user information from request headers or JWT claims.
/// For authenticated users, it verifies that X-Tenant-Id matches the tenant_id claim in the JWT.
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    private const string TenantIdHeaderName = "X-Tenant-Id";
    private const string ProjectIdHeaderName = "X-Project-Id";
    private const string TenantIdClaimType = "tenant_id";
    private const string ProjectIdClaimType = "project_id";
    private const string UserIdClaimType = "sub";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

        // 1. Extract tenant ID from the request header.
        var headerTenantId = ExtractFromHeader(context, TenantIdHeaderName);

        // 2. Extract tenant ID from JWT claims.
        GlobalUniqueId? jwtTenantId = null;
        if (isAuthenticated)
        {
            jwtTenantId = ExtractFromClaim(context.User, TenantIdClaimType);
        }

        // 3. Validate tenant ID: return 403 if the header and JWT values differ.
        if (isAuthenticated && headerTenantId.HasValue && jwtTenantId.HasValue
            && headerTenantId.Value != jwtTenantId.Value)
        {
            _logger.LogWarning(
                "Tenant mismatch: header={HeaderTenantId}, jwt={JwtTenantId}",
                headerTenantId, jwtTenantId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    success = false,
                    errorCode = "FORBIDDEN",
                    errorMessage = "Tenant ID mismatch: you do not belong to this tenant"
                }, JsonOptions));
            return;
        }

        // 4. Resolve tenant ID: prefer the header, fall back to JWT.
        var tenantId = headerTenantId ?? jwtTenantId;

        // 5. Extract project ID: prefer the header, fall back to JWT claims.
        var projectId = ExtractFromHeader(context, ProjectIdHeaderName);
        if (!projectId.HasValue && isAuthenticated)
        {
            projectId = ExtractFromClaim(context.User, ProjectIdClaimType);
        }

        // 6. Extract user ID from the JWT sub claim.
        GlobalUniqueId? userId = null;
        if (isAuthenticated)
        {
            userId = ExtractFromClaim(context.User, UserIdClaimType)
                ?? ExtractFromClaim(context.User, ClaimTypes.NameIdentifier);
        }

        // 7. Populate the tenant context.
        tenantContext.SetTenantId(tenantId);
        tenantContext.SetProjectId(projectId);
        tenantContext.SetUserId(userId);

        // 8. Store the values in HttpContext.Items for convenience.
        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId;
        }
        if (projectId.HasValue)
        {
            context.Items["ProjectId"] = projectId;
        }

        if (tenantId.HasValue)
        {
            _logger.LogDebug(
                "Tenant context set: TenantId={TenantId}, ProjectId={ProjectId}, UserId={UserId}",
                tenantId, projectId, userId);
        }

        await _next(context);
    }

    private static GlobalUniqueId? ExtractFromHeader(HttpContext context, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue) &&
            GlobalUniqueId.TryParse(headerValue!, out var id))
        {
            return id;
        }
        return null;
    }

    private static GlobalUniqueId? ExtractFromClaim(ClaimsPrincipal user, string claimType)
    {
        var claimValue = user.FindFirst(claimType)?.Value;
        if (!string.IsNullOrWhiteSpace(claimValue) &&
            GlobalUniqueId.TryParse(claimValue, out var id))
        {
            return id;
        }
        return null;
    }
}

/// <summary>
/// Tenant context middleware extension methods.
/// </summary>
public static class TenantContextMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant context middleware to the pipeline.
    /// Call this after UseAuthentication().
    /// </summary>
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantContextMiddleware>();
    }
}
