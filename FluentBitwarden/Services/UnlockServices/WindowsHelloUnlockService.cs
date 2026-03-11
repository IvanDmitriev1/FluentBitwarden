using System.Security.Cryptography;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class WindowsHelloUnlockService(
    ILocalVaultKeyManager localVaultKeyManager,
    ILocalVaultStateStore stateStore,
    ISessionManager sessionManager,
    WindowsHelloVerificationPrompt verificationPrompt)
    : IWindowsHelloUnlockService
{
    public ValueTask<bool> CanSetupAsync(CancellationToken cancellationToken = default)
        => verificationPrompt.CanPromptAsync(cancellationToken);

    public ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
        => stateStore.HasWindowsHelloEnrollmentAsync(session.AccountId, cancellationToken);

    public async ValueTask<VaultConfigurationOutcome> SetupAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        if (!await CanSetupAsync(cancellationToken).ConfigureAwait(false))
        {
            return new VaultConfigurationOutcome.Unavailable("Windows Hello is not available on this device.");
        }

        byte[] localVaultKey = localVaultKeyManager.GetUnlockedLocalVaultKeyCopy();

        try
        {
            LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            VaultConfigurationProtectionResult protectedLocalVaultKey = await ProtectLocalVaultKeyAsync(localVaultKey, cancellationToken).ConfigureAwait(false);
            if (protectedLocalVaultKey.Outcome is not null)
            {
                return protectedLocalVaultKey.Outcome;
            }

            LocalVaultState updatedState = state with
            {
                WindowsHello = protectedLocalVaultKey.State,
            };

            await stateStore.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
            return new VaultConfigurationOutcome.Success();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    public async ValueTask<VaultConfigurationOutcome> DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (state.WindowsHello is null)
        {
            return new VaultConfigurationOutcome.Success();
        }

        await stateStore.SaveAsync(state with { WindowsHello = null }, cancellationToken).ConfigureAwait(false);
        return new VaultConfigurationOutcome.Success();
    }

    public async ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        if (!await CanSetupAsync(cancellationToken).ConfigureAwait(false))
        {
            return new SessionUnlockOutcome.Unavailable("Windows Hello is not available on this device.");
        }

        LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        WindowsHelloLocalVaultKeyState helloState = state.WindowsHello
            ?? throw new InvalidOperationException("Windows Hello unlock is not enrolled for this session.");

        WindowsHelloVerificationOutcome verification = await verificationPrompt
            .VerifyAsync("Use Windows Hello to unlock your saved vault.", cancellationToken)
            .ConfigureAwait(false);

        switch (verification)
        {
            case WindowsHelloVerificationOutcome.Verified:
                break;
            case WindowsHelloVerificationOutcome.Cancelled cancelled:
                return new SessionUnlockOutcome.Cancelled(cancelled.Message);
            case WindowsHelloVerificationOutcome.Unavailable unavailable:
                return new SessionUnlockOutcome.Unavailable(unavailable.Message);
            default:
                throw new InvalidOperationException("Unsupported Windows Hello verification outcome.");
        }

        byte[]? localVaultKey = null;
        byte[]? userKey = null;

        try
        {
            localVaultKey = UnprotectLocalVaultKey(helloState);
            userKey = await localVaultKeyManager.DecryptUserKeyAsync(session.AccountId, localVaultKey, cancellationToken).ConfigureAwait(false);
            return await sessionManager.UnlockWithUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
            if (localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(localVaultKey);
            }
        }
    }

    private async ValueTask<VaultConfigurationProtectionResult> ProtectLocalVaultKeyAsync(
        byte[] localVaultKey,
        CancellationToken cancellationToken)
    {
        WindowsHelloVerificationOutcome verification = await verificationPrompt
            .VerifyAsync("Use Windows Hello to enable vault unlock.", cancellationToken)
            .ConfigureAwait(false);

        if (verification is not WindowsHelloVerificationOutcome.Verified)
        {
            VaultConfigurationOutcome outcome = verification switch
            {
                WindowsHelloVerificationOutcome.Cancelled cancelled => new VaultConfigurationOutcome.Cancelled(cancelled.Message),
                WindowsHelloVerificationOutcome.Unavailable unavailable => new VaultConfigurationOutcome.Unavailable(unavailable.Message),
                _ => new VaultConfigurationOutcome.Unavailable("Windows Hello verification failed."),
            };
            return new VaultConfigurationProtectionResult(null, outcome);
        }

        byte[] protectedBytes = ProtectedData.Protect(localVaultKey, null, DataProtectionScope.CurrentUser);

        try
        {
            return new VaultConfigurationProtectionResult(
                new WindowsHelloLocalVaultKeyState(Convert.ToBase64String(protectedBytes)),
                null);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private static byte[] UnprotectLocalVaultKey(WindowsHelloLocalVaultKeyState state)
    {
        byte[] protectedBytes = Convert.FromBase64String(state.ProtectedLocalVaultKey);

        try
        {
            return ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Windows Hello unlock data could not be decrypted.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private sealed record VaultConfigurationProtectionResult(
        WindowsHelloLocalVaultKeyState? State,
        VaultConfigurationOutcome? Outcome);
}
