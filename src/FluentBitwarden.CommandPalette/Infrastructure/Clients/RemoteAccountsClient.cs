using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile[]>(IpcMessageTypes.Account.GetAccounts, cancellationToken);

    public ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountLoginRequest, AccountLoginOutcome>(request, cancellationToken);
}
