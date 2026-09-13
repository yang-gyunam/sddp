namespace Sddp.Abstractions.Exceptions;

/// <summary>Raised only after password verification; no session has been issued.</summary>
public sealed class TenantSelectionRequiredException(IReadOnlyList<string> tenantIds)
    : SddpException("TENANT_SELECTION_REQUIRED", "Select a tenant to continue signing in.")
{
    public IReadOnlyList<string> TenantIds { get; } = tenantIds;
}
