using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Api.Constants;
using Sddp.Api.Services;
using Sddp.Application.Telemetry;
using Sddp.Domain.Events;
using Sddp.Infrastructure.Jobs.Quartz;
using Sddp.Infrastructure.Persistence;
using Sddp.Infrastructure.Persistence.Contexts;
using Sddp.Infrastructure.Persistence.Interceptors;
using Sddp.Infrastructure.Persistence.Outbox;
using Sddp.Infrastructure.Persistence.Projections;
using Sddp.Infrastructure.Persistence.Repositories;
using Sddp.Infrastructure.Services;

namespace Sddp.Api.Extensions;

/// <summary>
/// Service registration extension methods for the public runtime.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<VersioningInterceptor>();
        services.AddScoped<DomainEventOutboxInterceptor>();

        services.AddDbContext<SddpDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(SddpDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            });

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<VersioningInterceptor>(),
                sp.GetRequiredService<DomainEventOutboxInterceptor>());
        });

        services.AddScoped<IDbConnection>(_ =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            return new NpgsqlConnection(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(DapperRepository<>));

        services.Configure<GitRepositoryOptions>(configuration.GetSection("Git"));
        services.AddScoped<IArtifactRepositoryService, GitArtifactRepositoryService>();

        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IOutboxMessageHandler, ProjectionOutboxMessageHandler>();
        services.AddHostedService<OutboxBackgroundService>();

        services.AddScoped<IProjectSnapshotService, ProjectSnapshotService>();

        services.AddScoped<IProjectionDispatcher, ProjectionDispatcher>();
        services.AddScoped<IProjectionHandler<SpecStatusChangedEvent>, SpecStatusChangedProjectionHandler>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
        var issuer = jwtSection["Issuer"] ?? "sddp-api";
        var audience = jwtSection["Audience"] ?? "sddp-client";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    if (path.StartsWithSegments("/hubs"))
                    {
                        var accessToken = context.Request.Cookies[AuthCookieNames.HubAccessToken];
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("X-Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var configuredOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        var defaultOrigins = new[] { "http://localhost:3500", "http://localhost:5173", "http://localhost:9010" };
        var allowedOrigins = BuildAllowedOrigins(configuredOrigins, defaultOrigins);

        services.AddCors(options =>
        {
            var allowedHeaders = new[]
            {
                "Content-Type",
                "Authorization",
                "X-Correlation-Id",
                "X-Tenant-Id",
                "X-Project-Id",
                "X-Requested-With",
                "X-SignalR-User-Agent"
            };
            var allowedMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };

            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                       .WithHeaders(allowedHeaders)
                       .WithMethods(allowedMethods)
                       .AllowCredentials();
            });
        });

        return services;
    }

    private static string[] BuildAllowedOrigins(string[] configuredOrigins, string[] defaultOrigins)
    {
        var baseOrigins = configuredOrigins.Length > 0 ? configuredOrigins : defaultOrigins;
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var origin in baseOrigins)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                continue;
            }

            var normalized = origin.Trim().TrimEnd('/');
            resolved.Add(normalized);

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (!IsLoopbackHost(uri.Host))
            {
                continue;
            }

            var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
            resolved.Add($"{uri.Scheme}://localhost{port}");
            resolved.Add($"{uri.Scheme}://127.0.0.1{port}");
            resolved.Add($"{uri.Scheme}://[::1]{port}");
        }

        return resolved.ToArray();
    }

    private static bool IsLoopbackHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "SDDP API";
                document.Info.Version = "v1";
                document.Info.Description = "Spec-Driven Design Platform API\n\n" +
                    "**Authentication**: Include a Bearer token in the Authorization header.\n" +
                    "Example: `Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`";

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IServiceCollection AddTokenStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSection = configuration.GetSection("Redis");
        var connectionString = redisSection["ConnectionString"] ?? "localhost:6379";
        var instanceName = redisSection["InstanceName"] ?? "sddp:";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = instanceName;
        });

        services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();

        return services;
    }

    public static IServiceCollection AddQuartzServices(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();

            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 2;
            });

            var slaJobKey = new JobKey("approval-sla-monitor");
            q.AddJob<ApprovalSlaMonitorQuartzJob>(opts => opts.WithIdentity(slaJobKey).StoreDurably());
            q.AddTrigger(opts => opts
                .ForJob(slaJobKey)
                .WithIdentity("approval-sla-monitor-trigger")
                .WithCronSchedule("0 */15 * * * ?"));

            q.AddJobListener<RetryJobListener>();
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddSingleton<IJobScheduler, QuartzJobScheduler>();

        return services;
    }

    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("sddp-api"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource(MediatRActivitySource.Source.Name)
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)));

        return services;
    }
}
