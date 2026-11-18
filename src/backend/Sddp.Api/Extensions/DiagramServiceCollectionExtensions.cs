using MediatR;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models.Diagrams.V2;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Diagrams;
using Sddp.Infrastructure.Services.Diagrams;

namespace Sddp.Api.Extensions;

public static class DiagramServiceCollectionExtensions
{
    public static IServiceCollection AddDiagramServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RelationshipReadSnapshot<,>));
        services.AddSingleton<IStoredDiagramReader, StoredDiagramReader>();
        services.AddSingleton<DiagramListModelCache>();
        services.AddScoped<DiagramOriginScopeReader>();
        services.AddScoped<IDiagramOriginScopeReader>(sp => sp.GetRequiredService<DiagramOriginScopeReader>());
        services.AddScoped<IRelationshipEndpointReader>(sp => sp.GetRequiredService<DiagramOriginScopeReader>());
        services.AddScoped<IDiagramListReader, DiagramListReader>();
        services.AddScoped<DiagramRelationshipAccess>();
        services.AddScoped<IRelationshipEndpointAccess>(sp => sp.GetRequiredService<DiagramRelationshipAccess>());
        services.AddScoped<IDiagramOriginAccess>(sp => sp.GetRequiredService<DiagramRelationshipAccess>());
        services.AddScoped<DiagramProjection>();
        services.AddSingleton<IDiagramParser2, MermaidSubsetParser2>();
        return services;
    }
}
