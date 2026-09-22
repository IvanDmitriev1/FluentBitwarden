using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public ValueTask<AccountProfile[]> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountProfile[]>(IpcMessageTypes.Account.GetAccounts, cancellationToken);

    public ValueTask<AccountAuthenticationOutcome> AuthenticateAsync(AccountAuthenticationRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountAuthenticationRequest, AccountAuthenticationOutcome>(request, cancellationToken);
}
