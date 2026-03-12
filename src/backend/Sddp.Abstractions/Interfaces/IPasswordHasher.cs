namespace Sddp.Abstractions.Interfaces;

/// <summary>
///
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// .
    /// </summary>
    /// <param name="password"> </param>
    /// <returns> </returns>
    string Hash(string password);

    /// <summary>
    /// .
    /// </summary>
    /// <param name="password"> </param>
    /// <param name="hash"> </param>
    /// <returns> </returns>
    bool Verify(string password, string hash);
}
