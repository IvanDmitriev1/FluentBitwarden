using FluentBitwarden.AppHost.Modules.Account.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class AppSessionHostedService(
    IServiceScopeFactory serviceScopeFactory,
    AppSessionStore appSessionStore) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var accounts = accountService.GetAccounts();

        //We need change later when we will support more than one account
        if (accounts.Length > 0)
        {
            await appSessionStore.AuthenticateAsync(accounts[0], cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
