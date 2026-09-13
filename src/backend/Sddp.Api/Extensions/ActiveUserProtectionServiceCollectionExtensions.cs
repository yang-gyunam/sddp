using MediatR;
using Sddp.Abstractions.Interfaces;
using Sddp.Api.Hubs;
using Sddp.Api.Services;
using Sddp.Application.Behaviors;
using Sddp.Infrastructure.Services;

namespace Sddp.Api.Extensions;

/// <summary>
/// Service registration for active-user request and live-connection protection.
/// This extension lives outside the general service-registration file so public-source
/// overlays cannot accidentally replace the security contract.
/// </summary>
public static class ActiveUserProtectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the complete active-user request and live-connection protection contract.
    /// </summary>
    public static IServiceCollection AddActiveUserProtection(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddScoped<ActiveUserValidator>();
        services.AddScoped<IActiveUserGate>(
            provider => provider.GetRequiredService<ActiveUserValidator>());
        services.AddSingleton<IUserConnectionRegistry, UserConnectionRegistry>();
        services.AddSingleton<ActiveUserStatusLocalSink>();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IActiveUserStatusNotifier, LocalActiveUserStatusNotifier>();
        }
        else
        {
            services.AddSingleton<RedisActiveUserStatusNotifier>();
            services.AddSingleton<IActiveUserStatusNotifier>(
                provider => provider.GetRequiredService<RedisActiveUserStatusNotifier>());
            services.AddHostedService(
                provider => provider.GetRequiredService<RedisActiveUserStatusNotifier>());
        }

        // Registered before TransactionBehavior so the work after next() is post-commit. The
        // generic constraint makes DI skip this behavior for all unrelated requests.
        services.AddScoped(
            typeof(IPipelineBehavior<,>),
            typeof(AccountStatusPropagationBehavior<,>));

        return services;
    }
}
