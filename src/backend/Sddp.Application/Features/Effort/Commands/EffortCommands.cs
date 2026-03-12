using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Effort;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Effort.Commands;

/// <summary>
/// create/update (Upsert)
/// </summary>
public sealed record UpsertEffortAllocationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    UpsertEffortAllocationRequest Request) : ICommand<EffortAllocationDto>, IAuditableRequest<EffortAllocationDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(EffortAllocationDto response) => AuditLog;
}

public sealed class UpsertEffortAllocationCommandHandler : IRequestHandler<UpsertEffortAllocationCommand, EffortAllocationDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpsertEffortAllocationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EffortAllocationDto> Handle(UpsertEffortAllocationCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<EffortAllocation>();
        var userId = GlobalUniqueId.FromGuid(request.Request.UserId);
        var date = DateOnly.Parse(request.Request.Date);

        var existing = (await (repo.FindAsync(
            a => a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.UserId == userId
                && a.AllocationDate == date,
            cancellationToken)).ConfigureAwait(false)).FirstOrDefault();

        if (existing != null)
        {
            existing.UpdateAllocatedHours(request.Request.AllocatedHours, request.UserId);
            await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            existing = new EffortAllocation(
                request.TenantId,
                request.ProjectId,
                userId,
                date,
                request.Request.AllocatedHours,
                request.UserId);
            await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "upsert",
            "effort_allocation",
            existing.Id,
            new { existing.AllocationDate, existing.AllocatedHours },
            request.TenantId,
            request.ProjectId);

        return EffortMapping.MapToAllocationDto(existing);
    }
}

/// <summary>
/// create/update
/// </summary>
public sealed record BulkUpsertEffortAllocationsCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    BulkEffortAllocationRequest Request) : ICommand<List<EffortAllocationDto>>, IAuditableRequest<List<EffortAllocationDto>>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(List<EffortAllocationDto> response) => AuditLog;
}

public sealed class BulkUpsertEffortAllocationsCommandHandler : IRequestHandler<BulkUpsertEffortAllocationsCommand, List<EffortAllocationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public BulkUpsertEffortAllocationsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EffortAllocationDto>> Handle(BulkUpsertEffortAllocationsCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<EffortAllocation>();
        var results = new List<EffortAllocationDto>();

        foreach (var allocation in request.Request.Allocations)
        {
            var userId = GlobalUniqueId.FromGuid(allocation.UserId);
            var date = DateOnly.Parse(allocation.Date);

            var existing = (await (repo.FindAsync(
                a => a.TenantId == request.TenantId
                    && a.ProjectId == request.ProjectId
                    && a.UserId == userId
                    && a.AllocationDate == date,
                cancellationToken)).ConfigureAwait(false)).FirstOrDefault();

            if (existing != null)
            {
                existing.UpdateAllocatedHours(allocation.AllocatedHours, request.UserId);
                await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                existing = new EffortAllocation(
                    request.TenantId,
                    request.ProjectId,
                    userId,
                    date,
                    allocation.AllocatedHours,
                    request.UserId);
                await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
            }

            results.Add(EffortMapping.MapToAllocationDto(existing));
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "bulk_upsert",
            "effort_allocation",
            GlobalUniqueId.Empty,
            new { Count = results.Count },
            request.TenantId,
            request.ProjectId);

        return results;
    }
}

/// <summary>
/// delete
/// </summary>
public sealed record DeleteEffortAllocationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId AllocationId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteEffortAllocationCommandHandler : IRequestHandler<DeleteEffortAllocationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEffortAllocationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteEffortAllocationCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<EffortAllocation>();

        var allocation = await (repo.GetByIdAsync(request.AllocationId, cancellationToken)).ConfigureAwait(false);
        if (allocation == null || allocation.TenantId != request.TenantId || allocation.ProjectId != request.ProjectId)
        {
            return false;
        }

        await (repo.DeleteAsync(allocation, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "delete",
            "effort_allocation",
            request.AllocationId,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// log create
/// </summary>
public sealed record CreateWorklogCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    CreateWorklogRequest Request) : ICommand<WorklogDto>, IAuditableRequest<WorklogDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(WorklogDto response) => AuditLog;
}

public sealed class CreateWorklogCommandHandler : IRequestHandler<CreateWorklogCommand, WorklogDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorklogCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WorklogDto> Handle(CreateWorklogCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Worklog>();

        var worklog = new Worklog(
            request.TenantId,
            request.ProjectId,
            GlobalUniqueId.FromGuid(request.Request.UserId),
            DateOnly.Parse(request.Request.Date),
            request.Request.SpentHours,
            request.Request.Note,
            request.Request.TaskId.HasValue ? GlobalUniqueId.FromGuid(request.Request.TaskId.Value) : null);

        await (repo.AddAsync(worklog, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "create",
            "worklog",
            worklog.Id,
            new { worklog.SpentHours, worklog.WorkDate },
            request.TenantId,
            request.ProjectId);

        return EffortMapping.MapToWorklogDto(worklog);
    }
}

/// <summary>
/// log update
/// </summary>
public sealed record UpdateWorklogCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId WorklogId,
    UpdateWorklogRequest Request) : ICommand<WorklogDto?>, IAuditableRequest<WorklogDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(WorklogDto? response) => AuditLog;
}

public sealed class UpdateWorklogCommandHandler : IRequestHandler<UpdateWorklogCommand, WorklogDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorklogCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WorklogDto?> Handle(UpdateWorklogCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Worklog>();

        var worklog = await (repo.GetByIdAsync(request.WorklogId, cancellationToken)).ConfigureAwait(false);
        if (worklog == null || worklog.TenantId != request.TenantId || worklog.ProjectId != request.ProjectId)
        {
            return null;
        }

        worklog.Update(
            request.Request.SpentHours,
            request.Request.Note,
            request.Request.TaskId.HasValue ? GlobalUniqueId.FromGuid(request.Request.TaskId.Value) : null);

        await (repo.UpdateAsync(worklog, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "update",
            "worklog",
            request.WorklogId,
            new { request.Request.SpentHours, request.Request.Note },
            request.TenantId,
            request.ProjectId);

        return EffortMapping.MapToWorklogDto(worklog);
    }
}

/// <summary>
/// log delete
/// </summary>
public sealed record DeleteWorklogCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId WorklogId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteWorklogCommandHandler : IRequestHandler<DeleteWorklogCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorklogCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteWorklogCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Worklog>();

        var worklog = await (repo.GetByIdAsync(request.WorklogId, cancellationToken)).ConfigureAwait(false);
        if (worklog == null || worklog.TenantId != request.TenantId || worklog.ProjectId != request.ProjectId)
        {
            return false;
        }

        await (repo.DeleteAsync(worklog, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "delete",
            "worklog",
            request.WorklogId,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// workday settings
/// </summary>
public sealed record SetWorkingDayCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    SetWorkingDayRequest Request) : ICommand<WorkingDayDto>, IAuditableRequest<WorkingDayDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(WorkingDayDto response) => AuditLog;
}

public sealed class SetWorkingDayCommandHandler : IRequestHandler<SetWorkingDayCommand, WorkingDayDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SetWorkingDayCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkingDayDto> Handle(SetWorkingDayCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<WorkingDay>();
        var date = DateOnly.Parse(request.Request.Date);

        var existing = (await (repo.FindAsync(
            wd => wd.TenantId == request.TenantId
                && wd.ProjectId == request.ProjectId
                && wd.WorkDate == date,
            cancellationToken)).ConfigureAwait(false)).FirstOrDefault();

        if (existing != null)
        {
            existing.Update(request.Request.Type, request.Request.Note, request.UserId);
            await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            existing = new WorkingDay(
                request.TenantId,
                request.ProjectId,
                date,
                request.Request.Type,
                request.Request.Note,
                request.UserId);
            await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "set",
            "working_day",
            existing.Id,
            new { existing.WorkDate, request.Request.Type },
            request.TenantId,
            request.ProjectId);

        return EffortMapping.MapToWorkingDayDto(existing);
    }
}

/// <summary>
/// workday settings
/// </summary>
public sealed record BulkSetWorkingDaysCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    BulkWorkingDaysRequest Request) : ICommand<List<WorkingDayDto>>, IAuditableRequest<List<WorkingDayDto>>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(List<WorkingDayDto> response) => AuditLog;
}

public sealed class BulkSetWorkingDaysCommandHandler : IRequestHandler<BulkSetWorkingDaysCommand, List<WorkingDayDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public BulkSetWorkingDaysCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<WorkingDayDto>> Handle(BulkSetWorkingDaysCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<WorkingDay>();
        var results = new List<WorkingDayDto>();

        foreach (var workingDay in request.Request.WorkingDays)
        {
            var date = DateOnly.Parse(workingDay.Date);

            var existing = (await (repo.FindAsync(
                wd => wd.TenantId == request.TenantId
                    && wd.ProjectId == request.ProjectId
                    && wd.WorkDate == date,
                cancellationToken)).ConfigureAwait(false)).FirstOrDefault();

            if (existing != null)
            {
                existing.Update(workingDay.Type, workingDay.Note, request.UserId);
                await (repo.UpdateAsync(existing, cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                existing = new WorkingDay(
                    request.TenantId,
                    request.ProjectId,
                    date,
                    workingDay.Type,
                    workingDay.Note,
                    request.UserId);
                await (repo.AddAsync(existing, cancellationToken)).ConfigureAwait(false);
            }

            results.Add(EffortMapping.MapToWorkingDayDto(existing));
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "bulk_set",
            "working_day",
            GlobalUniqueId.Empty,
            new { Count = results.Count },
            request.TenantId,
            request.ProjectId);

        return results;
    }
}
