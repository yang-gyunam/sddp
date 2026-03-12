using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Persistence.Projections;

/// <summary>
/// Projection (Read Model) default
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Projection ID
    /// </summary>
    GlobalUniqueId Id { get; }

    /// <summary>
    ///
    /// </summary>
    Timestamp UpdatedAt { get; }
}

/// <summary>
/// Projection default
/// </summary>
public abstract class ProjectionBase : IProjection
{
    public GlobalUniqueId Id { get; protected set; } = GlobalUniqueId.NewId();
    public Timestamp UpdatedAt { get; protected set; } = Timestamp.Now;

    protected void MarkAsUpdated()
    {
        UpdatedAt = Timestamp.Now;
    }
}
