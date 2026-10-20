using Microsoft.AspNetCore.DataProtection;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Exceptions;
namespace Sddp.Api.Configuration;
public sealed class GitCredentialProtector(IDataProtectionProvider provider) : IGitCredentialProtector
{
    public string Protect(string token, string purpose) => provider.CreateProtector("SDDP.GitCredentials.v1", purpose).Protect(token);
    public string Unprotect(string ciphertext, string purpose)
    {
        try { return provider.CreateProtector("SDDP.GitCredentials.v1", purpose).Unprotect(ciphertext); }
        catch (System.Security.Cryptography.CryptographicException)
        { throw new SddpException("GIT_CREDENTIAL_UNAVAILABLE", "Git credentials cannot be decrypted. Ask an administrator to replace the token."); }
    }
}
