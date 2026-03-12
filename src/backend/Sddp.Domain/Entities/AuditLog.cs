using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Audit log entity.
/// Records important actions performed within the system.
/// </summary>
public class AuditLog : EntityBase
{
    /// <summary>
    /// Actor ID. Nullable for system-initiated actions.
    /// </summary>
    public GlobalUniqueId? ActorId { get; private set; }

    /// <summary>
    /// Performed action (for example, "Created", "Approved", "Rejected", "Locked").
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// Target resource type (for example, "Spec", "Conversation", "User").
    /// </summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>
    /// Target resource ID.
    /// </summary>
    public GlobalUniqueId ResourceId { get; private set; }

    /// <summary>
    /// Additional information in JSON form.
    /// Can store before/after values, reasons, and similar details.
    /// </summary>
    public string? Payload { get; private set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User-Agent value.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Request correlation ID.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Tenant ID for data isolation.
    /// </summary>
    public GlobalUniqueId? TenantId { get; private set; }

    /// <summary>
    /// Project ID for data isolation.
    /// </summary>
    public GlobalUniqueId? ProjectId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        GlobalUniqueId? actorId,
        string action,
        string resourceType,
        GlobalUniqueId resourceId,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        GlobalUniqueId? tenantId = null,
        GlobalUniqueId? projectId = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty", nameof(action));

        if (string.IsNullOrWhiteSpace(resourceType))
            throw new ArgumentException("Resource type cannot be empty", nameof(resourceType));

        return new AuditLog
        {
            ActorId = actorId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Payload = payload,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            TenantId = tenantId,
            ProjectId = projectId
        };
    }

    /// <summary>
    /// Predefined action constants.
    /// </summary>
    public static class Actions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Deleted = "Deleted";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Locked = "Locked";
        public const string Unlocked = "Unlocked";
        public const string Submitted = "Submitted";
        public const string Reviewed = "Reviewed";
        public const string Commented = "Commented";
        public const string Assigned = "Assigned";
        public const string Unassigned = "Unassigned";
        public const string LoggedIn = "LoggedIn";
        public const string LoggedOut = "LoggedOut";
        public const string PasswordChanged = "PasswordChanged";
        public const string RoleAssigned = "RoleAssigned";
        public const string RoleRemoved = "RoleRemoved";
        public const string StatusChanged = "StatusChanged";
    }
}
