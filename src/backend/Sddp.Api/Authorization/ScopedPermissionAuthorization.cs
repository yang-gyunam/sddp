using Microsoft.AspNetCore.Authorization;
using Sddp.Api.Services;

namespace Sddp.Api.Authorization;

public static class PermissionPolicyNames
{
    private const string Prefix = "Permission:";

    public static string For(string permissionCode) => $"{Prefix}{permissionCode}";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
        Policy = PermissionPolicyNames.For(permissionCode);
    }

    public string PermissionCode { get; }
}

/// <summary>
/// Marks an unsafe HTTP action that is intentionally exempt from scoped permission metadata.
/// Exemptions are audited by <c>MutationAuthorizationMetadataTests</c> and must be documented
/// in the mutation authorization contract.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MutationAuthorizationExemptAttribute : Attribute
{
    public MutationAuthorizationExemptAttribute(string reason, bool allowsAutomatedCaller = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Reason = reason;
        AllowsAutomatedCaller = allowsAutomatedCaller;
    }

    public string Reason { get; }

    /// <summary>
    /// Whether this action is also exempt from the AI-agent write guard because it does not persist data.
    /// </summary>
    public bool AllowsAutomatedCaller { get; }
}

public sealed class ScopedPermissionRequirement : IAuthorizationRequirement
{
    public ScopedPermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}

public sealed class ScopedPermissionHandler : AuthorizationHandler<ScopedPermissionRequirement>
{
    private readonly IScopedAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScopedPermissionHandler(
        IScopedAuthorizationService authorizationService,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopedPermissionRequirement requirement)
    {
        if (!_tenantContext.UserId.HasValue || !_tenantContext.TenantId.HasValue)
        {
            return;
        }

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted
            ?? CancellationToken.None;
        var hasPermission = await _authorizationService.HasPermissionAsync(
            _tenantContext.UserId.Value,
            _tenantContext.TenantId.Value,
            _tenantContext.ProjectId,
            requirement.PermissionCode,
            cancellationToken);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
