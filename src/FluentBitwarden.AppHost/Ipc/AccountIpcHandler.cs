using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class AccountIpcHandler(IAccountService accountService, IAppSessionOperations appSessionOperations) : IAccountClient, IIpcRequestsHandler
{
    public Task<AccountProfile[]> GetAccountsAsync(
        GetAccountsRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(accountService.GetAccounts());

    public async Task<AccountAuthenticationOutcome> AuthenticateAsync(
        AccountAuthenticationRequest request, CancellationToken cancellationToken = default)
    {
        AccountAuthenticationOutcome outcome = await accountService.AuthenticateAsync(request, cancellationToken);
        if (outcome is AccountAuthenticationOutcome.Success success)
            await appSessionOperations.AuthenticateAsync(success.Account, cancellationToken);

        return outcome;
    }
}
