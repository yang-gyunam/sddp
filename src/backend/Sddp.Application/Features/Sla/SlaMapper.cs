using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Sla;

internal static class SlaMapper
{
    internal static SlaPolicyDto MapToDto(SlaPolicy policy)
    {
        return new SlaPolicyDto(
            Id: policy.Id.ToString(),
            TenantId: policy.TenantId.ToString(),
            ProjectId: policy.ProjectId.ToString(),
            SlaType: policy.SlaType,
            SlaHours: policy.SlaHours,
            UrgentSlaHours: policy.UrgentSlaHours,
            ReminderAtPercent: policy.ReminderAtPercent,
            EscalationRole: policy.EscalationRole,
            CreatedAt: policy.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: policy.UpdatedAt.ToDateTimeOffset());
    }
}
