using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class AppHostClient(IIpcClient ipcClient)
{
    public async ValueTask<bool> IsVaultUnlockedAsync(CancellationToken cancellationToken) =>
        await ipcClient.SendAsync<AccountProfile?>(
            IpcMessageTypes.Account.GetUnlocked,
            cancellationToken) is not null;

    public ValueTask<VaultCipher[]> SearchLoginsAsync(
        string searchText,
        CancellationToken cancellationToken)
    {
        var query = new VaultCipherQuery
        {
            SearchText = searchText,
            CipherType = VaultCipherType.Login,
            Limit = 50,
            SortField = VaultCipherSortField.Name,
            SortDirection = VaultCipherSortDirection.Ascending,
        };

        return ipcClient.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);
    }

    public bool IsVaultUnlocked(TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        try
        {
            var account = ipcClient
                .SendAsync<AccountProfile?>(
                    IpcMessageTypes.Account.GetUnlocked,
                    cancellationTokenSource.Token)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return account is not null;
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
            return false;
        }
    }

    private static bool IsConnectionFailure(Exception exception) =>
        exception is IOException
            or TimeoutException
            or UnauthorizedAccessException
            or OperationCanceledException;
}
