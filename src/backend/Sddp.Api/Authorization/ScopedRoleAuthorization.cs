using Microsoft.AspNetCore.Authorization;
using Sddp.Abstractions.Enums;
using Sddp.Api.Services;

namespace Sddp.Api.Authorization;

public sealed class ScopedRoleRequirement : IAuthorizationRequirement
{
    public ScopedRoleRequirement(IReadOnlyCollection<RoleType> roleTypes)
    {
        RoleTypes = roleTypes;
    }

    public IReadOnlyCollection<RoleType> RoleTypes { get; }
}

public sealed class ScopedRoleHandler : AuthorizationHandler<ScopedRoleRequirement>
{
    private readonly IScopedAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScopedRoleHandler(
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
        ScopedRoleRequirement requirement)
    {
        if (!_tenantContext.UserId.HasValue || !_tenantContext.TenantId.HasValue)
        {
            return;
        }

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted
            ?? CancellationToken.None;
        var hasRole = await _authorizationService.HasAnyRoleAsync(
            _tenantContext.UserId.Value,
            _tenantContext.TenantId.Value,
            _tenantContext.ProjectId,
            requirement.RoleTypes,
            cancellationToken);

        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }
}
