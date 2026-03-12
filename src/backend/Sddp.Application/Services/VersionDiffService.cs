using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Services;

/// <summary>
/// Version comparison service implementation.
/// REQ-01.4: version comparison.
/// </summary>
public class VersionDiffService : IVersionDiffService
{
    private readonly IUnitOfWork _unitOfWork;

    public VersionDiffService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDiffResultDto> CompareSpecVersionsAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId1,
        GlobalUniqueId specId2,
        CancellationToken cancellationToken = default)
    {
        var repo = _unitOfWork.Repository<Spec>();

        var spec1 = await (repo.GetByIdAsync(specId1, cancellationToken)).ConfigureAwait(false);
        var spec2 = await (repo.GetByIdAsync(specId2, cancellationToken)).ConfigureAwait(false);

        if (spec1 is null || spec2 is null)
        {
            throw new SddpException("DIFF_ERROR", "One or both specs not found");
        }

        if (spec1.TenantId != tenantId || spec1.ProjectId != projectId
            || spec2.TenantId != tenantId || spec2.ProjectId != projectId)
        {
            throw new SddpException("DIFF_ERROR", "Specs do not belong to the specified tenant/project");
        }

        var changes = new List<FieldDiffDto>();

        // Compare each field.
        changes.Add(CompareField("title", "Title", spec1.Title, spec2.Title));
        changes.Add(CompareField("description", "Description", spec1.Description, spec2.Description));
        changes.Add(CompareField("decision", "Decision", spec1.Decision, spec2.Decision));
        changes.Add(CompareField("context", "Context", spec1.Context, spec2.Context));
        changes.Add(CompareField("scope", "Scope", spec1.Scope, spec2.Scope));
        changes.Add(CompareField("outOfScope", "Out of Scope", spec1.OutOfScope, spec2.OutOfScope));
        changes.Add(CompareField("definitions", "Definitions", spec1.Definitions, spec2.Definitions));
        changes.Add(CompareField("acceptanceCriteria", "Acceptance Criteria", spec1.AcceptanceCriteria, spec2.AcceptanceCriteria));
        changes.Add(CompareField("owners", "Owners", spec1.Owners.ToNullableCsv(), spec2.Owners.ToNullableCsv()));
        changes.Add(CompareField("reviewTrigger", "Review Trigger", spec1.ReviewTrigger, spec2.ReviewTrigger));
        changes.Add(CompareField("status", "Status", spec1.Status.ToString(), spec2.Status.ToString()));
        changes.Add(CompareField("requirementId", "Requirement",
            spec1.RequirementId?.ToString(), spec2.RequirementId?.ToString()));

        // Calculate change statistics.
        var addedCount = changes.Count(c => c.ChangeType == DiffChangeType.Added);
        var removedCount = changes.Count(c => c.ChangeType == DiffChangeType.Removed);
        var modifiedCount = changes.Count(c => c.ChangeType == DiffChangeType.Modified);

        var jsonDiff = JsonDiffUtility.Diff(ToSnapshot(spec1), ToSnapshot(spec2));

        return new SpecDiffResultDto(
            Spec1Id: spec1.Id.ToString(),
            Spec1Version: spec1.Version.ToString(),
            Spec2Id: spec2.Id.ToString(),
            Spec2Version: spec2.Version.ToString(),
            Changes: changes,
            AddedCount: addedCount,
            RemovedCount: removedCount,
            ModifiedCount: modifiedCount,
            JsonDiff: jsonDiff);
    }

    private static FieldDiffDto CompareField(string fieldName, string fieldLabel, string? oldValue, string? newValue)
    {
        // Normalize whitespace and null values.
        var normalizedOld = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
        var normalizedNew = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();

        DiffChangeType changeType;

        if (normalizedOld == normalizedNew)
        {
            changeType = DiffChangeType.Unchanged;
        }
        else if (normalizedOld == null && normalizedNew != null)
        {
            changeType = DiffChangeType.Added;
        }
        else if (normalizedOld != null && normalizedNew == null)
        {
            changeType = DiffChangeType.Removed;
        }
        else
        {
            changeType = DiffChangeType.Modified;
        }

        return new FieldDiffDto(
            FieldName: fieldName,
            FieldLabel: fieldLabel,
            OldValue: normalizedOld,
            NewValue: normalizedNew,
            ChangeType: changeType);
    }

    private static SpecDiffSnapshot ToSnapshot(Spec spec)
    {
        return new SpecDiffSnapshot(
            Title: Normalize(spec.Title),
            Description: Normalize(spec.Description),
            Decision: Normalize(spec.Decision),
            Context: Normalize(spec.Context),
            Scope: Normalize(spec.Scope),
            OutOfScope: Normalize(spec.OutOfScope),
            Definitions: Normalize(spec.Definitions),
            AcceptanceCriteria: Normalize(spec.AcceptanceCriteria),
            Owners: spec.Owners.GetEntries(),
            ReviewTrigger: Normalize(spec.ReviewTrigger),
            Status: spec.Status.ToString(),
            RequirementId: spec.RequirementId?.ToString());
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record SpecDiffSnapshot(
        string? Title,
        string? Description,
        string? Decision,
        string? Context,
        string? Scope,
        string? OutOfScope,
        string? Definitions,
        string? AcceptanceCriteria,
        IReadOnlyList<string> Owners,
        string? ReviewTrigger,
        string Status,
        string? RequirementId);
}
