using MediatR;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Requests;

public interface ICommand<out TResponse> : IRequest<TResponse> { }

public interface IQuery<out TResponse> : IRequest<TResponse> { }

public interface IAuditableRequest<in TResponse>
{
    AuditLogRequest? GetAuditLogRequest(TResponse response);
}

public sealed record AuditLogRequest(
    GlobalUniqueId? ActorId,
    string Action,
    string ResourceType,
    GlobalUniqueId ResourceId,
    object? Payload,
    GlobalUniqueId? TenantId,
    GlobalUniqueId? ProjectId);
