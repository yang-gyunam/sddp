namespace Sddp.Domain.Enums;

/// <summary>
/// Type of generated artifact
/// </summary>
public enum ArtifactType
{
    /// <summary>
    /// Domain entity class
    /// </summary>
    Entity = 0,

    /// <summary>
    /// Create DTO
    /// </summary>
    CreateDto = 1,

    /// <summary>
    /// Update DTO
    /// </summary>
    UpdateDto = 2,

    /// <summary>
    /// Response DTO
    /// </summary>
    ResponseDto = 3,

    /// <summary>
    /// FluentValidation validator class
    /// </summary>
    Validator = 4,

    /// <summary>
    /// EF Core Repository
    /// </summary>
    Repository = 5,

    /// <summary>
    /// Dapper Read Repository
    /// </summary>
    DapperReadRepository = 6,

    /// <summary>
    /// CQRS Command + Handler
    /// </summary>
    Command = 7,

    /// <summary>
    /// CQRS Query + Handler
    /// </summary>
    Query = 8,

    /// <summary>
    /// Unit test
    /// </summary>
    UnitTest = 9,

    /// <summary>
    /// API endpoint
    /// </summary>
    Endpoints = 10,

    /// <summary>
    /// Service class
    /// </summary>
    Service = 11,

    /// <summary>
    /// DB migration
    /// </summary>
    Migration = 12,

    /// <summary>
    /// Entity JSON definition (.entity.json)
    /// </summary>
    EntityJson = 13,

    /// <summary>
    /// UI component (Svelte 5)
    /// </summary>
    UiComponent = 14,

    /// <summary>
    /// Documentation (Markdown, etc.)
    /// </summary>
    Documentation = 15,

    /// <summary>
    /// EF Core Configuration
    /// </summary>
    Configuration = 16
}
