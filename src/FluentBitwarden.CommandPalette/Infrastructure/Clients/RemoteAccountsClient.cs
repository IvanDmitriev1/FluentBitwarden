using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public Task<AccountProfile[]> GetAccountsAsync(
        GetAccountsRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetAccountsRequest, AccountProfile[]>(request, cancellationToken);

    public Task<AccountAuthenticationOutcome> AuthenticateAsync(
        AccountAuthenticationRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountAuthenticationRequest, AccountAuthenticationOutcome>(request, cancellationToken);
}
