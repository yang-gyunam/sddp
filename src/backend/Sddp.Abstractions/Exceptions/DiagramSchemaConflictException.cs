namespace Sddp.Abstractions.Exceptions;

/// <summary>The caller must opt into the V2 contract to access a schema2 document.</summary>
public sealed class DiagramSchemaConflictException() : SddpException("UNSUPPORTED_MODEL_SCHEMA", "This diagram requires the V2 API contract.");
