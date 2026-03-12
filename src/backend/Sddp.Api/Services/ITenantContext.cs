using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Services;

/// <summary>
/// Tenant context interface.
/// Provides tenant, project, and user information for the current request in a multi-tenant environment.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant ID.
    /// </summary>
    GlobalUniqueId? TenantId { get; }

    /// <summary>
    /// Current project ID.
    /// </summary>
    GlobalUniqueId? ProjectId { get; }

    /// <summary>
    /// Current user ID extracted from the JWT sub claim.
    /// </summary>
    GlobalUniqueId? UserId { get; }

    /// <summary>
    /// Indicates whether the tenant context is valid.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    void SetTenantId(GlobalUniqueId? tenantId);

    /// <summary>
    /// Sets the project ID.
    /// </summary>
    void SetProjectId(GlobalUniqueId? projectId);

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    void SetUserId(GlobalUniqueId? userId);
}

/// <summary>
/// Tenant context implementation.
/// Registered as a scoped service so each request gets its own instance.
/// </summary>
public class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public GlobalUniqueId? TenantId { get; private set; }

    /// <inheritdoc />
    public GlobalUniqueId? ProjectId { get; private set; }

    /// <inheritdoc />
    public GlobalUniqueId? UserId { get; private set; }

    /// <inheritdoc />
    public bool IsValid => TenantId.HasValue;

    /// <inheritdoc />
    public void SetTenantId(GlobalUniqueId? tenantId)
    {
        TenantId = tenantId;
    }

    /// <inheritdoc />
    public void SetProjectId(GlobalUniqueId? projectId)
    {
        ProjectId = projectId;
    }

    /// <inheritdoc />
    public void SetUserId(GlobalUniqueId? userId)
    {
        UserId = userId;
    }
}
