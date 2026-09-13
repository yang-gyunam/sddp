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
    /// Whether login is currently locked for this account.
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Time at which the temporary login lock expires.
    /// </summary>
    public Timestamp? LockedUntil { get; private set; }

    /// <summary>
    /// Consecutive failed login attempts since the last successful login or expired lock.
    /// </summary>
    public int FailedLoginCount { get; private set; }

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
        RecordLogin(Timestamp.Now);
    }

    public void RecordLogin(Timestamp timestamp)
    {
        LastLoginAt = timestamp;
        FailedLoginCount = 0;
        IsLocked = false;
        LockedUntil = null;
        MarkAsModified();
    }

    public bool IsLoginLocked(Timestamp timestamp)
    {
        return IsLocked && (!LockedUntil.HasValue || LockedUntil.Value > timestamp);
    }

    public void ResetExpiredLoginLockout(Timestamp timestamp)
    {
        if (!IsLocked || !LockedUntil.HasValue || LockedUntil.Value > timestamp)
        {
            return;
        }

        FailedLoginCount = 0;
        IsLocked = false;
        LockedUntil = null;
        MarkAsModified();
    }

    public void RecordFailedLogin(Timestamp timestamp, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        if (maxFailedAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        ResetExpiredLoginLockout(timestamp);
        FailedLoginCount++;

        if (FailedLoginCount >= maxFailedAttempts)
        {
            IsLocked = true;
            LockedUntil = Timestamp.FromDateTimeOffset(
                timestamp.ToDateTimeOffset().Add(lockoutDuration));
        }

        MarkAsModified();
    }
}
