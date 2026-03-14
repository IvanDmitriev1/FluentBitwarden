using System.Security.Cryptography;

namespace BitwaredApi.Models.Auth;

public sealed record MasterPasswordAuth(
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
