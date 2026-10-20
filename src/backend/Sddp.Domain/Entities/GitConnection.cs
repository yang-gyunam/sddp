using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Abstractions.Exceptions;

namespace Sddp.Domain.Entities;

public sealed class GitConnection : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public string Name { get; private set; } = "";
    public string Provider { get; private set; } = "";
    public string ServerUrl { get; private set; } = "";
    public string Username { get; private set; } = "";
    public string? ProtectedToken { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset? CheckedAt { get; private set; }
    private GitConnection() { }
    public GitConnection(GlobalUniqueId tenantId, GlobalUniqueId projectId, string name, string provider, string serverUrl)
    { TenantId = tenantId; ProjectId = projectId; Name = name; Provider = provider; ServerUrl = serverUrl; }
    public void CheckVersion(int expected)
    { if (Version != expected) throw new ConflictException("Git connection changed. Reload before saving."); }
    public void Update(string name, string username, string? protectedToken, DateTimeOffset? checkedAt)
    { Name = name; Username = username; ProtectedToken = protectedToken; CheckedAt = checkedAt; Version++; MarkAsModified(); }
    public override void Deactivate() { base.Deactivate(); Version++; }
}
