using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Browser;
using FluentBitwarden.Contracts.Modules.Vault.Models;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal sealed class BrowserClientHandlers(
    IVaultWorkspace vaultWorkspace,
    IUnlockedVaultReader unlockedVaultReader,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IIpcRequestsHandler
{
    public ValueTask<BrowserVaultStatusResponse> GetStatusAsync(
        BrowserVaultStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var isUnlocked = unlockedAccountAccessor.HasUnlockedAccount && vaultWorkspace.IsOpen;
        return ValueTask.FromResult(new BrowserVaultStatusResponse(IsAvailable: true, isUnlocked));
    }

    public ValueTask<BrowserCredentialAvailabilityResponse> GetCredentialAvailabilityAsync(
        BrowserCredentialAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsVaultOpen())
        {
            return ValueTask.FromResult(new BrowserCredentialAvailabilityResponse(
                VaultLocked: true,
                Count: 0,
                Items: []));
        }

        if (!request.HasPasswordField || !TryGetOrigin(request, out var origin))
        {
            return ValueTask.FromResult(new BrowserCredentialAvailabilityResponse(
                VaultLocked: false,
                Count: 0,
                Items: []));
        }

        var items = FindMatchingLoginCiphers(origin)
            .Select(static x => new BrowserCredentialListItem(
                x.Id.Value,
                x.Username,
                x.Name))
            .ToArray();

        return ValueTask.FromResult(new BrowserCredentialAvailabilityResponse(
            VaultLocked: false,
            items.Length,
            items));
    }

    public ValueTask<BrowserCredentialFillResponse> GetCredentialFillAsync(
        BrowserCredentialFillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsVaultOpen() ||
            !request.UserGesture ||
            string.IsNullOrWhiteSpace(request.ItemId) ||
            !TryGetOrigin(request, out var origin))
        {
            return ValueTask.FromResult(new BrowserCredentialFillResponse(null, null));
        }

        var cipher = unlockedVaultReader.GetCipher(new CipherId(request.ItemId));

        if (cipher is not LoginVaultCipher loginCipher || !MatchesOrigin(loginCipher, origin))
        {
            return ValueTask.FromResult(new BrowserCredentialFillResponse(null, null));
        }

        return ValueTask.FromResult(new BrowserCredentialFillResponse(
            loginCipher.Username,
            loginCipher.Password));
    }

    private bool IsVaultOpen() => unlockedAccountAccessor.HasUnlockedAccount && vaultWorkspace.IsOpen;

    private IEnumerable<LoginVaultCipher> FindMatchingLoginCiphers(Uri origin)
    {
        var query = new VaultCipherQuery
        {
            CipherType = CipherType.Login,
            IncludeDeleted = false,
            IncludeArchived = false,
        };

        return unlockedVaultReader
            .GetCiphers(query)
            .OfType<LoginVaultCipher>()
            .Where(x => MatchesOrigin(x, origin));
    }

    private static bool MatchesOrigin(LoginVaultCipher cipher, Uri origin) =>
        cipher.Uris.Any(uri => TryCreateAbsoluteUri(uri, out var candidate) && SameOrigin(origin, candidate));

    private static bool TryGetOrigin(BrowserCredentialAvailabilityRequest request, out Uri origin) =>
        TryGetOrigin(request.Origin, request.Url, out origin);

    private static bool TryGetOrigin(BrowserCredentialFillRequest request, out Uri origin) =>
        TryGetOrigin(request.Origin, request.Url, out origin);

    private static bool TryGetOrigin(string originValue, string urlValue, out Uri origin)
    {
        if (TryCreateAbsoluteUri(originValue, out var originUri) ||
            TryCreateAbsoluteUri(urlValue, out originUri))
        {
            origin = originUri;
            return !string.IsNullOrWhiteSpace(origin.Host);
        }

        origin = null!;
        return false;
    }

    private static bool TryCreateAbsoluteUri(string value, [NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host);

    private static bool SameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase) &&
        left.Port == right.Port;
}
