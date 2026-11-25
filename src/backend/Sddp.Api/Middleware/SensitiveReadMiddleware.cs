using Microsoft.AspNetCore.Http.Features;
using Sddp.Api.Security;
using Sddp.Api.Services;

namespace Sddp.Api.Middleware;

internal sealed class SensitiveReadMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, ITenantContext tenant, SensitiveReadMonitor monitor)
    {
        var endpoint = context.GetEndpoint()?.Metadata.GetMetadata<SensitiveReadAttribute>();
        var original = context.Features.Get<IHttpResponseBodyFeature>();
        if (endpoint is null || original is null || !monitor.IsEnabled || !HttpMethods.IsGet(context.Request.Method)
            || context.User.Identity?.IsAuthenticated != true || tenant.TenantId is not { } tenantId
            || tenant.UserId is not { } actor)
            return next(context);

        return CountAsync(context, original, bytes => monitor.Observe(new(tenantId.ToGuid(), actor.ToGuid(),
            tenant.ProjectId?.ToGuid(), endpoint.Surface, bytes, DateTimeOffset.UtcNow)));
    }

    private async Task CountAsync(HttpContext context, IHttpResponseBodyFeature original, Action<long> observe)
    {
        var counted = new CountingResponseBody(original);
        context.Features.Set<IHttpResponseBodyFeature>(counted);
        var returned = false;
        try
        {
            await next(context);
            returned = true;
        }
        finally
        {
            if (ReferenceEquals(context.Features.Get<IHttpResponseBodyFeature>(), counted))
                context.Features.Set(original);

            var bytes = counted.Bytes;
            if (bytes > 0 && context.Response.StatusCode is >= 200 and < 300 && (returned || context.Response.HasStarted))
                observe(bytes);
        }
    }
}
