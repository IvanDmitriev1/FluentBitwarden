using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Session.Models.Authentication;

public sealed record PasswordSignInContinuation(
    string Email,
    byte[] MasterKey,
    byte[] StretchedMasterKey,
    string ServerAuthorizationHash) : IDisposable
{
    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(MasterKey);
        CryptographicOperations.ZeroMemory(StretchedMasterKey);
    }
}