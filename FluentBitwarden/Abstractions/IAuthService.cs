using BitwaredApi.Models.Auth;
using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Abstractions;

public interface IAuthService
{
    ValueTask<StoredSessionInfo?> GetStoredSessionAsync(CancellationToken cancellationToken = default);

    ValueTask<PreloginResponseModel> PreloginAsync(string email, CancellationToken cancellationToken = default);

    ValueTask<AuthSession> SignInWithPasswordAsync(
        string email,
        string masterPassword,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> ContinueTwoFactorAsync(
        string token,
        TwoFactorProviderType provider,
        bool remember,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> UnlockWithMasterPasswordAsync(
        string masterPassword,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> UnlockWithUserKeyAsync(
        byte[] userKey,
        CancellationToken cancellationToken = default);

    ValueTask<byte[]?> ExportUserKeyAsync(CancellationToken cancellationToken = default);

    ValueTask<string> EnsureAccessTokenAsync(CancellationToken cancellationToken = default);

    ValueTask<PendingDeviceLogin> StartDeviceLoginAsync(
        string email,
        CancellationToken cancellationToken = default);

    ValueTask<AuthSession> WaitForDeviceApprovalAsync(
        PendingDeviceLogin pendingRequest,
        CancellationToken cancellationToken = default);

    ValueTask LockAsync(CancellationToken cancellationToken = default);

    ValueTask LogoutAsync(CancellationToken cancellationToken = default);
}
