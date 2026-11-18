using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Api.Authorization;
using Sddp.Api.Services;

namespace Sddp.Api.Controllers;

[Route("api/system/statistics")]
[Authorize(Policy = "RequireAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StatisticsController(WeeklyStatistics statistics) : BaseApiController
{
    [HttpGet]
    [ResourceScopeExempt("Tenant-wide statistics require a tenant administrator without a project scope.")]
    public async Task<IActionResult> Read(CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenant) || TryGetProjectId(out _)) return Forbid();
        return Ok(ApiResponse<StatisticsView>.Ok(await statistics.Read(tenant.ToGuid(), ct)));
    }

    public sealed record Consent(bool Enabled, string? Recipient, int? RetentionDays);

    [HttpPut("consent")]
    [Authorize]
    [RequirePermission(PermissionCodes.UserManage)]
    [ResourceScopeExempt("Tenant-wide sharing is controlled by a tenant administrator in tenant-only scope.")]
    public async Task<IActionResult> Set([FromBody] Consent consent, CancellationToken ct)
    {
        if (!TryGetTenantId(out var tenant) || TryGetProjectId(out _) || !TryGetUserId(out var user)) return Forbid();
        return await statistics.Set(tenant.ToGuid(), user.ToGuid(), consent.Enabled, consent.Recipient, consent.RetentionDays, ct)
            ? NoContent() : Conflict(ApiResponse<object>.Fail("STATISTICS_RECIPIENT_CHANGED", "Review the recipient and retention period again."));
    }
}
