using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Hubs;

public sealed class ActiveUserHubFilter : IHubFilter
{
    private readonly IUserConnectionRegistry _connections;

    public ActiveUserHubFilter(IUserConnectionRegistry connections)
    {
        _connections = connections;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);

        await EnsureActiveAsync(invocationContext.Context, invocationContext.ServiceProvider)
            .ConfigureAwait(false);

        return await next(invocationContext).ConfigureAwait(false);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var userId = GetUserIdOrAbort(context.Context);

        // Register before checking the gate. If a deactivation event races this handshake, either
        // it aborts the registered connection or the gate observes the distributed marker. There
        // is no untracked interval in which both protections can miss the connection.
        _connections.Register(userId, context.Context.ConnectionId, context.Context.Abort);
        try
        {
            await EnsureActiveAsync(context.Context, context.ServiceProvider, userId)
                .ConfigureAwait(false);
            await next(context).ConfigureAwait(false);
        }
        catch
        {
            _connections.Unregister(context.Context.ConnectionId);
            throw;
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        _connections.Unregister(context.Context.ConnectionId);
        await next(context, exception).ConfigureAwait(false);
    }

    internal static async ValueTask EnsureActiveAsync(HubCallerContext caller, IServiceProvider services)
    {
        var userId = GetUserIdOrAbort(caller);
        await EnsureActiveAsync(caller, services, userId).ConfigureAwait(false);
    }

    private static GlobalUniqueId GetUserIdOrAbort(HubCallerContext caller)
    {
        // Same claim order the rest of the pipeline reads it in.
        var subject = caller.User?.FindFirst("sub")?.Value
            ?? caller.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (GlobalUniqueId.TryParse(subject, out var userId))
        {
            return userId;
        }

        caller.Abort();
        throw new HubException("This connection has no usable identity.");
    }

    private static async ValueTask EnsureActiveAsync(
        HubCallerContext caller,
        IServiceProvider services,
        GlobalUniqueId userId)
    {

        // GetRequiredService, not GetService: nothing to check against means nothing was checked,
        // and failing loudly beats letting the call through as though it had been.
        var gate = services.GetRequiredService<IActiveUserGate>();

        if (!await gate.IsActiveAsync(userId, caller.ConnectionAborted).ConfigureAwait(false))
        {
            caller.Abort();
            throw new HubException("The account behind this connection is no longer active.");
        }
    }
}
