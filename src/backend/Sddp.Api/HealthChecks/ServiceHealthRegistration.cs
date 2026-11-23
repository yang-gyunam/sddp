using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.HealthChecks;

public static class ServiceHealthRegistration
{
    public static void AddServiceStatus(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient("ServiceHealth").ConfigurePrimaryHttpMessageHandler(() =>
            new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, PooledConnectionLifetime = TimeSpan.FromMinutes(2) });
        services.AddSingleton<IServiceHealthReader, ServiceHealthReader>();
        services.AddSingleton(new ServiceProbe("web", "Web frontend", "Application", "Nginx health endpoint",
            config["ServiceHealth:WebUrl"], Required: true));
        services.AddSingleton(new ServiceProbe("git", "Gitea", "Integrations", "Git server version endpoint",
            config["Git:ServerUrl"], Path: "/api/v1/version"));
        services.AddSingleton(new ServiceProbe("speech", "Whisper", "Integrations", "Speech server health endpoint",
            config["SpeechToText:Url"], Enabled: config.GetValue<bool>("FeatureFlags:Speech:Enabled"), Path: "/health"));
        services.AddSingleton(new ServiceProbe("telemetry", "Telemetry collector", "Observability", "OTLP port connection",
            config["OpenTelemetry:Endpoint"], Tcp: true));
        foreach (var server in config.GetSection("ServiceHealth:Servers").GetChildren())
        {
            services.AddSingleton(new ServiceProbe($"instance-{server.Key}", server["Name"] ?? server.Key,
                "Instances", "HTTP health endpoint", server["Url"], Required: server.GetValue<bool>("Required")));
        }
    }
}
