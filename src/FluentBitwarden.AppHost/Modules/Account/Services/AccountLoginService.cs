using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountLoginService()
{
    public Task LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
