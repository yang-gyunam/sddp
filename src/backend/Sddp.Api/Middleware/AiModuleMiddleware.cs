using Sddp.Abstractions.DTOs;
using Sddp.Api.Configuration;

namespace Sddp.Api.Middleware;

/// <summary>Marks controllers whose action metadata requires the optional module.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RequiresAiModuleAttribute : Attribute;

/// <summary>Marks HTTP operations that require deployment execution to be enabled.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresAiExecutionAttribute : Attribute;

/// <summary>Deployment gate only; scoped authorization and execution guards remain authoritative.</summary>
public sealed class AiModuleMiddleware(RequestDelegate next, DistributionInfo distribution)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (distribution.AiInstalled && !distribution.DeploymentEnabled
            && context.GetEndpoint()?.Metadata.GetMetadata<RequiresAiExecutionAttribute>() is not null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.Fail("AI_DISABLED", "AI execution is disabled on this server."),
                context.RequestAborted);
            return;
        }
        await next(context);
    }
}
