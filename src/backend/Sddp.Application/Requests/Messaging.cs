using MediatR;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Application.Requests;

public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Marks a command that owns its persistence boundary. Login uses this marker so rejected attempts can be
/// committed before the handler returns an authentication error.
/// </summary>
public interface INonTransactionalCommand { }

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
