using System.Collections.ObjectModel;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Base;

/// <summary>
/// Base class for all entities.
/// </summary>
public abstract class EntityBase
{
    private readonly List<object> _domainEvents = [];

    /// <summary>
    /// Globally unique identifier for the entity (UUIDv7).
    /// </summary>
    public GlobalUniqueId Id { get; protected set; } = GlobalUniqueId.NewId();

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public Timestamp CreatedAt { get; protected set; } = Timestamp.Now;

    /// <summary>
    /// Last update timestamp in UTC.
    /// </summary>
    public Timestamp UpdatedAt { get; protected set; } = Timestamp.Now;

    /// <summary>
    /// Indicates whether the entity is active for soft-delete support.
    /// </summary>
    public bool IsActive { get; protected set; } = true;

    /// <summary>
    /// Updates the modification timestamp after an entity change.
    /// </summary>
    protected void MarkAsModified()
    {
        UpdatedAt = Timestamp.Now;
    }

    /// <summary>
    /// Soft-deletes the entity.
    /// </summary>
    public virtual void Deactivate()
    {
        IsActive = false;
        MarkAsModified();
    }

    /// <summary>
    /// Reactivates the entity.
    /// </summary>
    public virtual void Activate()
    {
        IsActive = true;
        MarkAsModified();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Id.IsEmpty || other.Id.IsEmpty)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(EntityBase? left, EntityBase? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(EntityBase? left, EntityBase? right) => !(left == right);

    /// <summary>
    /// Domain events queued for publication.
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => new ReadOnlyCollection<object>(_domainEvents);

    /// <summary>
    /// Adds a domain event to the pending queue.
    /// </summary>
    protected void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events after publishing.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
