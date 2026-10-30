using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.IntegrationTests.AppSession;

public sealed class AppSessionHostedServiceTests
{
    [Fact]
    public async Task Startup_with_no_accounts_leaves_session_unauthenticated()
    {
        await using var provider = CreateProvider([]);
        var store = provider.GetRequiredService<AppSessionStore>();
        var service = new AppSessionHostedService(provider.GetRequiredService<IServiceScopeFactory>(), store);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.IsType<AppSessionStateSnapshot.NotAuthenticated>(store.State);
    }

    [Fact]
    public async Task Startup_selects_only_first_account_and_stays_locked()
    {
        var first = AccountTestData.Profile(AccountTestData.FirstUserId, "first@example.test", "first");
        var second = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        await using var provider = CreateProvider([first, second]);
        var store = provider.GetRequiredService<AppSessionStore>();
        var service = new AppSessionHostedService(provider.GetRequiredService<IServiceScopeFactory>(), store);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(first, Assert.IsType<AppSessionStateSnapshot.Locked>(store.State).Account);
    }

    [Fact]
    public async Task Startup_disposes_its_scoped_account_service_when_reading_accounts_fails()
    {
        var accountService = Substitute.For<IAccountService, IDisposable>();
        accountService.GetAccounts().Returns(_ => throw new InvalidOperationException("read failed"));
        await using var provider = CreateProvider(accountService);
        var service = new AppSessionHostedService(provider.GetRequiredService<IServiceScopeFactory>(), new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        ((IDisposable)accountService).Received(1).Dispose();
    }

    private static ServiceProvider CreateProvider(AccountProfile[] accounts)
    {
        var accountService = Substitute.For<IAccountService, IDisposable>();
        accountService.GetAccounts().Returns(accounts);
        return CreateProvider(accountService);
    }

    private static ServiceProvider CreateProvider(IAccountService accountService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        services.AddSingleton<AppSessionStore>();
        services.AddScoped(_ => accountService);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
