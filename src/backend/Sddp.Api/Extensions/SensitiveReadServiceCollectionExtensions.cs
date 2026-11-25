using Sddp.Api.Security;

namespace Sddp.Api.Extensions;

/// <summary>Registers optional monitoring for sensitive reads in both editions.</summary>
public static class SensitiveReadServiceCollectionExtensions
{
    public static IServiceCollection AddSensitiveReadMonitoring(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddOptions<SensitiveReadOptions>().BindConfiguration(SensitiveReadOptions.Section)
            .Validate(options => !options.Enabled || options.Windows().All(window => window.ByteLimit > 0 && window.RequestLimit > 0),
                "Sensitive read thresholds must be positive.")
            .ValidateOnStart();
        services.AddSingleton<SensitiveReadMonitor>();
        // The HTTP test host replaces Redis and can install its own monitor fixture.
        if (!environment.IsEnvironment("Testing"))
            services.AddHostedService(provider => provider.GetRequiredService<SensitiveReadMonitor>());
        return services;
    }
}
