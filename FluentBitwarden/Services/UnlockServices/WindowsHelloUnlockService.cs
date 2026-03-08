using System.Security.Cryptography;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class WindowsHelloUnlockService(
    ILocalVaultUnlocker localVaultUnlocker,
    LocalVaultUnlockStateRepository stateRepository,
    LocalVaultSessionUnlocker sessionUnlocker,
    WindowsHelloVerificationPrompt verificationPrompt)
    : IWindowsHelloUnlockService
{
    public ValueTask<bool> CanSetupAsync(CancellationToken cancellationToken = default)
        => verificationPrompt.CanPromptAsync(cancellationToken);

    public ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
        => stateRepository.HasWindowsHelloEnrollmentAsync(session.AccountId, cancellationToken);

    public async ValueTask SetupAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        if (!await CanSetupAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Windows Hello is not available on this device.");
        }

        byte[] localVaultKey = localVaultUnlocker.GetUnlockedLocalVaultKeyCopy();

        try
        {
            LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            LocalVaultUnlockerState updatedState = state with
            {
                WindowsHello = await ProtectLocalVaultKeyAsync(localVaultKey, cancellationToken).ConfigureAwait(false),
            };

            await stateRepository.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    public async ValueTask DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (state.WindowsHello is null)
        {
            return;
        }

        await stateRepository.SaveAsync(state with { WindowsHello = null }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        WindowsHelloLocalVaultKeyState helloState = state.WindowsHello
            ?? throw new InvalidOperationException("Windows Hello unlock is not enrolled for this session.");

        byte[]? localVaultKey = null;

        try
        {
            localVaultKey = await UnprotectLocalVaultKeyAsync(helloState, cancellationToken).ConfigureAwait(false);
            return await sessionUnlocker.UnlockAsync(session.AccountId, localVaultKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(localVaultKey);
            }
        }
    }

    private async ValueTask<WindowsHelloLocalVaultKeyState> ProtectLocalVaultKeyAsync(
        byte[] localVaultKey,
        CancellationToken cancellationToken)
    {
        await verificationPrompt.VerifyAsync("Use Windows Hello to enable vault unlock.", cancellationToken).ConfigureAwait(false);
        byte[] protectedBytes = ProtectedData.Protect(localVaultKey, null, DataProtectionScope.CurrentUser);

        try
        {
            return new WindowsHelloLocalVaultKeyState(Convert.ToBase64String(protectedBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private async ValueTask<byte[]> UnprotectLocalVaultKeyAsync(
        WindowsHelloLocalVaultKeyState state,
        CancellationToken cancellationToken)
    {
        await verificationPrompt.VerifyAsync("Use Windows Hello to unlock your saved vault.", cancellationToken).ConfigureAwait(false);
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
}
