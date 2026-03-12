using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Commands;

/// <summary>
/// spec SignOff
/// </summary>
public sealed record SubmitSpecSignOffCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId,
    SubmitSignOffDto Dto) : ICommand<SignOffDto>, IAuditableRequest<SignOffDto>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SignOffDto response) => AuditLog;
}

public sealed class SubmitSpecSignOffCommandHandler : IRequestHandler<SubmitSpecSignOffCommand, SignOffDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SubmitSpecSignOffCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SignOffDto> Handle(SubmitSpecSignOffCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var signOffRepo = _unitOfWork.Repository<SignOff>();
        var userRepo = _unitOfWork.Repository<User>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            throw new SddpException("SIGN_OFF_ERROR", "Spec not found");
        }

        if (spec.Status != SpecStatus.InReview)
        {
            throw new SddpException("SIGN_OFF_ERROR", "Sign-off is only allowed for specs in InReview status");
        }

        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        if (user is null)
        {
            throw new SddpException("SIGN_OFF_ERROR", "User not found");
        }

        var existingSignOffs = await (signOffRepo.FindAsync(
            s => s.SpecId == request.SpecId
                && s.StakeholderId == request.UserId
                && s.TenantId == request.TenantId
                && s.ProjectId == request.ProjectId
                && s.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var existingSignOff = existingSignOffs.FirstOrDefault(s => s.Decision == SignOffDecision.Pending);

        var isNewSignOff = existingSignOff is null;
        var signOff = existingSignOff
            ?? new SignOff(request.TenantId, request.ProjectId, request.SpecId, request.UserId, RoleType.Developer);

        if (isNewSignOff)
        {
            await (signOffRepo.AddAsync(signOff, cancellationToken)).ConfigureAwait(false);
        }

        switch (request.Dto.Decision)
        {
            case SignOffDecision.Approved:
                signOff.Approve(request.Dto.Comments);
                break;
            case SignOffDecision.Rejected:
                signOff.Reject(request.Dto.Comments ?? "Rejected");
                break;
            case SignOffDecision.Conditional:
                if (string.IsNullOrWhiteSpace(request.Dto.Conditions))
                {
                    throw new SddpException("VALIDATION_ERROR", "Conditions are required for conditional approval");
                }
                signOff.ApproveWithConditions(request.Dto.Conditions, request.Dto.Comments);
                break;
            default:
                throw new SddpException("SIGN_OFF_ERROR", $"Invalid sign-off decision: {request.Dto.Decision}");
        }

        if (!isNewSignOff)
        {
            await (signOffRepo.UpdateAsync(signOff, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            AuditLog.Actions.Submitted,
            "sign-off",
            signOff.Id,
            new { SpecId = request.SpecId.ToString(), Decision = request.Dto.Decision.ToString(), request.Dto.Comments, request.Dto.Conditions },
            request.TenantId,
            request.ProjectId);

        return SpecMapping.MapToSignOffDto(signOff, new UserRefDto(user.Id.ToString(), user.DisplayName ?? user.Username, user.AvatarUrl));
    }
}
