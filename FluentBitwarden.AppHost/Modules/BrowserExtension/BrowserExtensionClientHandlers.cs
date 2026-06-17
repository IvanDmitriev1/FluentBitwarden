using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.BrowserExtension;
using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension;

internal sealed class BrowserExtensionClientHandlers(
    IVaultWorkspace vaultWorkspace,
    IUnlockedVaultReader unlockedVaultReader,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IBrowserExtensionClient, IIpcRequestsHandler
{
    private static readonly VaultCipherQuery LoginCipherQuery = new()
    {
        CipherType = VaultCipherType.Login
    };

    private bool CanReadVault => unlockedAccountAccessor.HasUnlockedAccount && vaultWorkspace.IsOpen;

    public ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var isUnlocked = unlockedAccountAccessor.HasUnlockedAccount && vaultWorkspace.IsOpen;
        return ValueTask.FromResult(new BrowserVaultStatusResponse(IsRunning: true, isUnlocked));
    }

    public ValueTask<BrowserCredentialAvailabilityResponse> CheckCredentialAvailabilityAsync(
        BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadVault || !BrowserPageUri.TryCreate(request.Url, out var pageUri))
            return ValueTask.FromResult(BrowserCredentialAvailabilityResponse.Empty);

        var items = unlockedVaultReader
            .GetCiphers(LoginCipherQuery)
            .OfType<LoginVaultCipher>()
            .Where(cipher => BrowserLoginUriMatcher.Matches(cipher, pageUri))
            .Select(static cipher => new BrowserCredentialListItem(cipher.Id, cipher.Username ?? string.Empty))
            .ToArray();

        return ValueTask.FromResult(new BrowserCredentialAvailabilityResponse(items));
    }

    public ValueTask<BrowserCredentialFillResponse> FillCredentialAsync(
        BrowserCredentialFillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadVault ||
            request.ItemId.IsEmpty ||
            request.Part is BrowserCredentialPart.None ||
            !BrowserPageUri.TryCreate(request.Url, out var pageUri))
        {
            return ValueTask.FromResult(BrowserCredentialFillResponse.Empty);
        }

        if (unlockedVaultReader.GetCipher(request.ItemId) is not LoginVaultCipher { DeletedDate: null } loginCipher ||
            !BrowserLoginUriMatcher.Matches(loginCipher, pageUri))
        {
            return ValueTask.FromResult(BrowserCredentialFillResponse.Empty);
        }

        var returnedParts = BrowserCredentialPart.None;
        string? username = null;
        string? password = null;
        string? totp = null;
        DateTimeOffset? totpExpiresAt = null;

        if (request.Part.Includes(BrowserCredentialPart.Username))
        {
            username = loginCipher.Username ?? string.Empty;
            returnedParts |= BrowserCredentialPart.Username;
        }

        if (request.Part.Includes(BrowserCredentialPart.Password))
        {
            password = loginCipher.Password ?? string.Empty;
            returnedParts |= BrowserCredentialPart.Password;
        }

        if (request.Part.Includes(BrowserCredentialPart.Totp) && loginCipher.Totp is { } totpValue)
        {
            totp = totpValue.ComputeTotp();
            totpExpiresAt = totpValue.ExpiresAt;
            returnedParts |= BrowserCredentialPart.Totp;
        }

        return ValueTask.FromResult(new BrowserCredentialFillResponse
        {
            ReturnedParts = returnedParts,
            Username = username,
            Password = password,
            Totp = totp,
            TotpExpiresAt = totpExpiresAt
        });
    }

}
