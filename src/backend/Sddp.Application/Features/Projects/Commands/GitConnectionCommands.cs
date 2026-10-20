using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
namespace Sddp.Application.Features.Projects.Commands;
public sealed record ListGitConnectionsQuery(GlobalUniqueId Tenant, GlobalUniqueId? Project) : IQuery<GitConnectionListDto>;
public sealed record CreateGitConnectionCommand(GlobalUniqueId Tenant, CreateGitConnectionDto Input) : ICommand<GitConnectionDto>;
public sealed record UpdateGitConnectionCommand(GlobalUniqueId Tenant, GlobalUniqueId Id, UpdateGitConnectionDto Input) : ICommand<GitConnectionDto>;
public sealed record DeleteGitConnectionCommand(GlobalUniqueId Tenant, GlobalUniqueId Id, int Version) : ICommand<bool>;
public sealed class GitConnectionCommandHandler(IGitConnectionService service) :
    IRequestHandler<ListGitConnectionsQuery, GitConnectionListDto>, IRequestHandler<CreateGitConnectionCommand, GitConnectionDto>,
    IRequestHandler<UpdateGitConnectionCommand, GitConnectionDto>, IRequestHandler<DeleteGitConnectionCommand, bool>
{
    public Task<GitConnectionListDto> Handle(ListGitConnectionsQuery r, CancellationToken ct) => service.ListAsync(r.Tenant, r.Project, ct);
    public Task<GitConnectionDto> Handle(CreateGitConnectionCommand r, CancellationToken ct) => service.CreateAsync(r.Tenant, r.Input, ct);
    public Task<GitConnectionDto> Handle(UpdateGitConnectionCommand r, CancellationToken ct) => service.UpdateAsync(r.Tenant, r.Id, r.Input, ct);
    public async Task<bool> Handle(DeleteGitConnectionCommand r, CancellationToken ct) { await service.DeleteAsync(r.Tenant, r.Id, r.Version, ct); return true; }
}
