using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class AccountIpcHandler(IAccountService accountService) : IAccountClient, IIpcRequestsHandler
{
    public Task<AccountProfile[]> GetAccountsAsync(
        GetAccountsRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(accountService.GetAccounts());

    public Task<AccountAuthenticationOutcome> AuthenticateAsync(
        AccountAuthenticationRequest request, CancellationToken cancellationToken = default) =>
        accountService.AuthenticateAsync(request, cancellationToken);
}
