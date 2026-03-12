using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Api.Extensions;
using Sddp.Api.HealthChecks;
using Sddp.Api.Hubs;
using Sddp.Api.Middleware;
using Sddp.Api.Serialization;
using Sddp.Api.Services;
using Sddp.Application.Behaviors;
using Sddp.Application.Requests;
using Sddp.Application.Services;
using Sddp.Infrastructure.Persistence.Contexts;
using Sddp.Infrastructure.Persistence.Seeding;
using Sddp.Infrastructure.Services;

// Bootstrap Serilog logger.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SDDP API...");

    var builder = WebApplication.CreateBuilder(args);

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Destructure.ByTransforming<LoginRequest>(r => new { r.Username, Password = "***" })
                .Destructure.ByTransforming<ChangePasswordRequest>(_ => new { CurrentPassword = "***", NewPassword = "***" })
                .Destructure.ByTransforming<RefreshTokenRequest>(_ => new { RefreshToken = "***" });

            var otlpEndpoint = context.Configuration["OpenTelemetry:Endpoint"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                configuration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = "sddp-api"
                    };
                });
            }
        });
    }

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddOpenTelemetryObservability(builder.Configuration);
    }

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IRequestContextProvider, HttpRequestContextProvider>();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddTokenStorage(builder.Configuration);
    }

    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<IVersionDiffService, VersionDiffService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IEmbeddingTriggerService, NoOpEmbeddingTriggerService>();
    builder.Services.AddScoped<IAnalysisTriggerService, NoOpAnalysisTriggerService>();

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ICommand<>).Assembly));
    builder.Services.AddValidatorsFromAssembly(typeof(ICommand<>).Assembly);
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
    builder.Services.AddScoped<IConversationHubService, ConversationHubService>();
    builder.Services.AddScoped<ISpecNotificationService, SpecNotificationService>();
    builder.Services.AddScoped<ITimelineNotificationService, TimelineNotificationService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddQuartzServices();
    }

    builder.Services.AddMemoryCache();

    var healthChecks = builder.Services.AddHealthChecks();
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        healthChecks
            .AddCheck<DatabaseReadinessHealthCheck>("database_readiness")
            .AddCheck<RedisConnectivityHealthCheck>("redis_connectivity")
            .AddCheck<SchemaVersionHealthCheck>("schema_version");
    }

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new TermCategoryJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<SddpDbContext>();

            var seeder = new DatabaseSeeder(
                context,
                services.GetRequiredService<ILogger<DatabaseSeeder>>());
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during database migration/seeding");
        }
    }

    app.UseRequestLogging();
    app.UseExceptionHandling();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };
        });
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors("DefaultPolicy");
    app.UseAuthentication();
    app.UseTenantContext();
    app.UseProjectMembership();
    app.UseAuthorization();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                entries = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                        data = e.Value.Data.Count > 0 ? e.Value.Data : null
                    })
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapControllers();
    app.MapHub<ConversationHub>("/hubs/conversation").RequireCors("DefaultPolicy");
    app.MapHub<DashboardHub>("/hubs/dashboard").RequireCors("DefaultPolicy");

    var appVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0";
    app.MapGet("/", () => Results.Ok(new
    {
        Name = "SDDP API",
        Version = appVersion,
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow
    }))
    .WithName("ApiInfo")
    .WithTags("Info")
    .AllowAnonymous();

    Log.Information("SDDP API started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
