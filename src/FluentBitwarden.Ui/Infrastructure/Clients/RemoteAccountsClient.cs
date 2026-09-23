using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteAccountsClient(IIpcClient ipcClient) : IAccountsClient
{
    public Task<AccountProfile[]> GetAccountsAsync(
        GetAccountsRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetAccountsRequest, AccountProfile[]>(request, cancellationToken);

    public Task<AccountAuthenticationOutcome> AuthenticateAsync(AccountAuthenticationRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<AccountAuthenticationRequest, AccountAuthenticationOutcome>(request, cancellationToken);
}
