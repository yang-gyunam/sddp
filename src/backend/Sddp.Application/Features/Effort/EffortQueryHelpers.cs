using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Effort;

internal static class EffortQueryHelpers
{
    internal static async Task<List<AllocationConflictDto>> GetUserConflictsAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var allocationRepo = unitOfWork.Repository<EffortAllocation>();
        var userRepo = unitOfWork.Repository<User>();
        var projectRepo = unitOfWork.Repository<Project>();

        var allAllocations = await (allocationRepo.FindAsync(
            a => a.TenantId == tenantId
                && a.UserId == userId
                && a.AllocationDate >= startDate
                && a.AllocationDate <= endDate,
            cancellationToken)).ConfigureAwait(false);

        var projectIds = allAllocations
            .Select(a => a.ProjectId)
            .Distinct()
            .ToList();

        var projectNameMap = projectIds.Count == 0
            ? new Dictionary<GlobalUniqueId, string>()
            : (await (projectRepo.FindAsync(
                    p => p.TenantId == tenantId && projectIds.Contains(p.Id),
                    cancellationToken)).ConfigureAwait(false))
                .ToDictionary(p => p.Id, p => p.Name);

        var user = await (userRepo.GetByIdAsync(userId, cancellationToken)).ConfigureAwait(false);
        var userName = user?.DisplayName ?? user?.Username ?? "Unknown";

        var conflictDates = allAllocations
            .GroupBy(a => a.AllocationDate)
            .Where(g => g.Sum(a => a.AllocatedHours) > 8)
            .Select(g => new AllocationConflictDto
            {
                UserId = userId.ToGuid(),
                UserName = userName,
                Date = g.Key.ToString("yyyy-MM-dd"),
                TotalAllocated = g.Sum(a => a.AllocatedHours),
                Projects = g.Select(a => new ProjectAllocationDto
                {
                    ProjectId = a.ProjectId.ToGuid(),
                    ProjectName = projectNameMap.TryGetValue(a.ProjectId, out var name)
                        ? name
                        : $"Project {a.ProjectId.ToGuid().ToString()[..8]}",
                    AllocatedHours = a.AllocatedHours
                }).ToList()
            })
            .ToList();

        return conflictDates;
    }
}
