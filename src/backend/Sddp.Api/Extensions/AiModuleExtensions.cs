using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Sddp.Abstractions.Interfaces;
using Sddp.Api.Configuration;
using Sddp.Api.Middleware;
using Sddp.Api.Services;

namespace Sddp.Api.Extensions;

/// <summary>Host-side optional module boundary; no types from the optional DLL are referenced here.</summary>
public static class AiModuleExtensions
{
    public static IServiceCollection AddSddpAiModule(this IServiceCollection services,
        IConfiguration configuration, DistributionInfo distribution,
        Action<IServiceCollection, IConfiguration>? registerModule = null)
    {
        services.AddSingleton(distribution);
        services.AddScoped<IEmbeddingTriggerService, UnavailableAiTriggers>();
        services.AddScoped<IAnalysisTriggerService, UnavailableAiTriggers>();
        if (distribution.AiInstalled)
        {
            if (registerModule is null)
                throw new InvalidOperationException("Installed AI module has no registration entry point.");
            registerModule(services, configuration);
        }
        services.AddControllers().ConfigureApplicationPartManager(parts =>
        {
            // MVC controllers must be public. Avoid enumerating private async state machines in
            // the host's optional adapters: their fields may reference an absent AI assembly.
            for (var i = 0; i < parts.ApplicationParts.Count; i++)
                if (parts.ApplicationParts[i] is AssemblyPart part && part.Assembly == typeof(AiModuleExtensions).Assembly)
                    parts.ApplicationParts[i] = new PublicControllerAssemblyPart(part.Assembly);
            // Exclude optional controllers before MVC reflects over their DLL-owned action DTOs.
            for (var i = 0; i < parts.FeatureProviders.Count; i++)
                if (parts.FeatureProviders[i] is ControllerFeatureProvider)
                    parts.FeatureProviders[i] = new OptionalAiControllerFeatureProvider(distribution);
        });
        return services;
    }

    public static IApplicationBuilder UseSddpAi(this IApplicationBuilder app)
    {
        var distribution = app.ApplicationServices.GetRequiredService<DistributionInfo>();
        return distribution.AiInstalled ? app.UseMiddleware<AiModuleMiddleware>() : app;
    }
}

internal sealed class PublicControllerAssemblyPart(Assembly assembly) : ApplicationPart, IApplicationPartTypeProvider
{
    public override string Name => assembly.GetName().Name!;
    public IEnumerable<TypeInfo> Types => assembly.GetExportedTypes().Select(type => type.GetTypeInfo());
}

internal sealed class OptionalAiControllerFeatureProvider(DistributionInfo distribution) : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo) =>
        (distribution.AiInstalled || !typeInfo.IsDefined(typeof(RequiresAiModuleAttribute), inherit: true))
        && base.IsController(typeInfo);
}
