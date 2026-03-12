using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Services;

/// <summary>
/// BCrypt-based password hashing service.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    // Work factor: 12 (recommended, about 0.25 seconds per hash).
    // Higher values improve security but increase hashing time.
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Treat malformed hashes as authentication failures.
            return false;
        }
    }
}
