using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// user entity
/// </summary>
public class User : EntityBase
{
    /// <summary>
    /// user (log ID)
    /// </summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    ///
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    ///
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    ///
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// URL
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Person ID
    /// </summary>
    public GlobalUniqueId PersonId { get; private set; }

    /// <summary>
    /// AI system
    /// </summary>
    public bool IsAI { get; private set; }

    /// <summary>
    /// log change
    /// </summary>
    public bool RequirePasswordChange { get; private set; }

    /// <summary>
    /// log
    /// </summary>
    public Timestamp? LastLoginAt { get; private set; }

    /// <summary>
    /// user settings (JSONB, raw JSON string)
    /// </summary>
    public string? Preferences { get; private set; }

    /// <summary>
    /// user role
    /// </summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    // EF Core default create
    private User() { }

    public User(string username, string email, string passwordHash, string displayName, bool isAI = false)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        IsAI = isAI;
    }

    public User(string username, string email, string passwordHash, string displayName, GlobalUniqueId personId, bool isAI = false)
        : this(username, email, passwordHash, displayName, isAI)
    {
        PersonId = personId;
    }

    public void UpdateProfile(string displayName, string email, string? avatarUrl = null)
    {
        DisplayName = displayName;
        Email = email;
        AvatarUrl = avatarUrl;
        MarkAsModified();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        RequirePasswordChange = false;
        MarkAsModified();
    }

    public void ForcePasswordChange()
    {
        RequirePasswordChange = true;
        MarkAsModified();
    }

    public void UpdatePreferences(string? preferences)
    {
        Preferences = preferences;
        MarkAsModified();
    }

    public void RecordLogin()
    {
        LastLoginAt = Timestamp.Now;
        MarkAsModified();
    }
}
