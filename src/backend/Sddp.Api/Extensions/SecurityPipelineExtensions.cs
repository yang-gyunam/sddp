using Sddp.Api.Middleware;

namespace Sddp.Api.Extensions;

/// <summary>
/// Defines the security-sensitive middleware order shared by the internal and public hosts.
/// </summary>
public static class SecurityPipelineExtensions
{
    /// <summary>
    /// Adds routing and the complete authentication, scope and authorization chain.
    /// </summary>
    /// <remarks>
    /// Routing must run first so endpoint metadata and route values are available to CORS, rate
    /// limiting and resource-scope resolution. Resource scope must run after tenant context and
    /// before membership/authorization so the owning project, rather than a caller-selected
    /// header, is authoritative for every downstream decision.
    /// </remarks>
    public static WebApplication UseSddpSecurityPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseCors("DefaultPolicy");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<RouteIdentifierMiddleware>();
        app.UseTenantContext();
        app.UseResourceScope();
        app.UseProjectMembership();
        app.UseAuthorization();

        return app;
    }
}
