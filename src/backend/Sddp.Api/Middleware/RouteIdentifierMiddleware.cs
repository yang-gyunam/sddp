using Microsoft.AspNetCore.Mvc.Controllers;
using Sddp.Api.Authorization;

namespace Sddp.Api.Middleware;

/// <summary>Validates identifiers after routing, so malformed IDs have a JSON 400 contract.</summary>
public sealed class RouteIdentifierMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (context.User.Identity?.IsAuthenticated == true && action is not null)
        {
            var keys = action.Parameters
                .Where(parameter => parameter.ParameterType == typeof(Guid)
                    || parameter.ParameterType == typeof(Guid?))
                .Select(parameter => parameter.BindingInfo?.BinderModelName ?? parameter.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var scope = endpoint!.Metadata.GetMetadata<IResourceScopeMetadata>();
            if (scope is not null)
            {
                keys.Add(scope.RouteKey);
                if (scope.ParentRouteKey is not null) keys.Add(scope.ParentRouteKey);
            }
            foreach (var key in keys)
            {
                // Query/body arguments are left to their existing binding contract.
                if (context.Request.RouteValues.TryGetValue(key, out var value)
                    && !Guid.TryParse(value?.ToString(), out _))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        errorCode = "INVALID_ID",
                        errorMessage = $"'{key}' is not a valid identifier."
                    }, context.RequestAborted);
                    return;
                }
            }
        }
        await next(context);
    }
}
