
using FluentBitwarden.AppHost.Application.Abstractions;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using static FluentBitwarden.AppHost.IntegrationTests.Modules.Account.AccountSessionAccessTokenProviderTestSupport;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountSessionAccessTokenProviderTests
{
    [Fact]
    public async Task Reuses_cached_tokens_across_DI_scopes()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens tokens = Tokens(accountContext, "shared");
        accountService.RefreshSessionTokensAsync(accountContext)
            .Returns(Task.FromResult(tokens));
        await using ServiceProvider services = CreateServices(accountService);
        using IServiceScope firstScope = services.CreateScope();
        using IServiceScope secondScope = services.CreateScope();

        SessionAccessToken first = await GetToken(Provider(firstScope), accountContext);
        SessionAccessToken second = await GetToken(Provider(secondScope), accountContext);

        Assert.Equal(tokens.AccessToken, first);
        Assert.Equal(first, second);
        await accountService.Received(1).RefreshSessionTokensAsync(accountContext);
    }

    [Fact]
    public async Task Separates_cached_tokens_by_account_and_environment()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext firstAccount = Context(AccountTestData.FirstUserId);
        BitwardenAccountContext secondAccount = Context(AccountTestData.SecondUserId);
        BitwardenAccountContext europeanAccount = Context(AccountTestData.FirstUserId, BitwardenEnvironment.Europe);
        AccountSessionTokens firstTokens = Tokens(firstAccount, "first-us");
        AccountSessionTokens secondTokens = Tokens(secondAccount, "second-us");
        AccountSessionTokens europeanTokens = Tokens(europeanAccount, "first-eu");
        accountService.RefreshSessionTokensAsync(firstAccount).Returns(Task.FromResult(firstTokens));
        accountService.RefreshSessionTokensAsync(secondAccount).Returns(Task.FromResult(secondTokens));
        accountService.RefreshSessionTokensAsync(europeanAccount).Returns(Task.FromResult(europeanTokens));
        await using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IAccessTokenProvider provider = Provider(scope);

        Assert.Equal(firstTokens.AccessToken, await GetToken(provider, firstAccount));
        Assert.Equal(secondTokens.AccessToken, await GetToken(provider, secondAccount));
        Assert.Equal(europeanTokens.AccessToken, await GetToken(provider, europeanAccount));

        await accountService.Received(1).RefreshSessionTokensAsync(firstAccount);
        await accountService.Received(1).RefreshSessionTokensAsync(secondAccount);
        await accountService.Received(1).RefreshSessionTokensAsync(europeanAccount);
    }

    [Fact]
    public async Task Refreshes_an_expired_cached_token_and_reuses_the_replacement()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens expiredTokens = Tokens(accountContext, "expired", DateTimeOffset.UtcNow.AddMinutes(4));
        AccountSessionTokens replacementTokens = Tokens(accountContext, "replacement");
        int refreshCount = 0;
        accountService.RefreshSessionTokensAsync(accountContext)
            .Returns(_ => Task.FromResult(Interlocked.Increment(ref refreshCount) == 1 ? expiredTokens : replacementTokens));
        await using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IAccessTokenProvider provider = Provider(scope);

        Assert.Equal(expiredTokens.AccessToken, await GetToken(provider, accountContext));
        Assert.Equal(replacementTokens.AccessToken, await GetToken(provider, accountContext));
        Assert.Equal(replacementTokens.AccessToken, await GetToken(provider, accountContext));
        Assert.Equal(2, refreshCount);
    }

    [Fact]
    public async Task Coordinates_simultaneous_refreshes_across_DI_scopes()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens tokens = Tokens(accountContext, "coordinated");
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;
        accountService.RefreshSessionTokensAsync(accountContext)
            .Returns(async call =>
            {
                Interlocked.Increment(ref refreshCount);
                refreshStarted.TrySetResult();
                await releaseRefresh.Task;
                return tokens;
            });
        await using ServiceProvider services = CreateServices(accountService);
        using IServiceScope firstScope = services.CreateScope();
        using IServiceScope secondScope = services.CreateScope();

        Task<SessionAccessToken> first = GetToken(Provider(firstScope), accountContext).AsTask();
        await WaitWithTimeoutAsync(refreshStarted.Task, TimeSpan.FromSeconds(5));
        Task<SessionAccessToken> second = GetToken(Provider(secondScope), accountContext).AsTask();
        Assert.False(second.IsCompleted);
        releaseRefresh.TrySetResult();

        SessionAccessToken[] results = await Task.WhenAll(first, second);

        Assert.All(results, token => Assert.Equal(tokens.AccessToken, token));
        Assert.Equal(1, refreshCount);
    }

    [Fact]
    public async Task Retries_after_refresh_failure_without_publishing_a_failed_result()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens tokens = Tokens(accountContext, "after-failure");
        int refreshCount = 0;
        accountService.RefreshSessionTokensAsync(accountContext)
            .Returns(_ => Interlocked.Increment(ref refreshCount) == 1
                ? Task.FromException<AccountSessionTokens>(new InvalidOperationException("Synthetic refresh failure."))
                : Task.FromResult(tokens));
        await using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IAccessTokenProvider provider = Provider(scope);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await GetToken(provider, accountContext));
        Assert.Equal(tokens.AccessToken, await GetToken(provider, accountContext));
        Assert.Equal(2, refreshCount);
    }

    [Fact]
    public async Task Lock_completes_while_token_refresh_is_paused_and_keeps_the_session_locked_for_same_account()
    {
        var accountService = Substitute.For<IAccountService>();
        var vaultService = Substitute.For<IVaultService>();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        accountService.GetAccount(account.UserId).Returns(account);
        accountService.UnlockKey(account.UserId, Arg.Any<AccountUnlockMethod>())
            .Returns(new AccountKeyUnlockResult.Success(new TestAccountKeySession(userKey)));
        vaultService.Open(
            Arg.Is<BitwardenAccountContext>(context => context == account.BitwardenAccountContext),
            Arg.Is<IAccountKeySession>(key => key.UserId == account.UserId)).Returns(vault);

        BitwardenAccountContext accountContext = Context(account.UserId.ToString());
        AccountSessionTokens tokens = Tokens(accountContext, "paused-refresh");
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        accountService.RefreshSessionTokensAsync(accountContext)
            .Returns(async call =>
            {
                refreshStarted.TrySetResult();
                await releaseRefresh.Task;
                return tokens;
            });

        await using ServiceProvider services = CreateServices(accountService, vaultService);
        using IServiceScope scope = services.CreateScope();
        IAppSessionOperations sessionOperations = scope.ServiceProvider.GetRequiredService<IAppSessionOperations>();
        var sessionStore = scope.ServiceProvider.GetRequiredService<AppSessionStore>();
        await sessionStore.AuthenticateAsync(account, TestContext.Current.CancellationToken);
        Assert.IsType<SessionUnlockOutcome.Success>(await sessionOperations.UnlockAsync(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password"),
            TestContext.Current.CancellationToken));
        IAccessTokenProvider provider = Provider(scope);
        Task<SessionAccessToken> refresh = GetToken(provider, accountContext).AsTask();
        await WaitWithTimeoutAsync(refreshStarted.Task, TimeSpan.FromSeconds(5));

        Task lockTask = sessionOperations.LockAsync(TestContext.Current.CancellationToken);
        await lockTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(refresh.IsCompleted, "The refresh remains paused until its release signal.");
        releaseRefresh.TrySetResult();
        await refresh;

        AppSessionStateSnapshot.Locked lockedState = Assert.IsType<AppSessionStateSnapshot.Locked>(sessionStore.State);
        Assert.Equal(account, lockedState.Account);
        Assert.Equal(tokens.AccessToken, await GetToken(provider, accountContext));
        Assert.Equal(lockedState, sessionStore.State);
    }

    [Fact]
    public async Task Registers_the_session_service_and_token_cache_with_the_required_lifetimes()
    {
        var services = new ServiceCollection();
        services.AddAccountModule();
        services.AddSingleton(Substitute.For<IIpcEventPublisher>());
        services.AddSingleton(Substitute.For<IAccountService>());
        services.AddSingleton(Substitute.For<IVaultService>());
        services.AddSingleton(Substitute.For<IAppHostTermination>());
        services.AddAppSessionModule();
        services.AddSingleton(Substitute.For<BitwardenApi.Notifications.Abstractions.IServerNotificationsDispatcherFactory>());
        var networkStatus = Substitute.For<FluentBitwarden.Platform.Infrastructure.Connectivity.INetworkStatus>();
        networkStatus.HasInternetAccess.Returns(false);
        services.AddSingleton(networkStatus);

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        IAppSessionOperations firstSession = firstScope.ServiceProvider.GetRequiredService<IAppSessionOperations>();
        IAppSessionOperations secondSession = secondScope.ServiceProvider.GetRequiredService<IAppSessionOperations>();
        IAccessTokenProvider firstTokenProvider = Provider(firstScope);
        IAccessTokenProvider secondTokenProvider = Provider(secondScope);

        Assert.IsType<AppSessionOperations>(firstSession);
        Assert.NotSame(firstSession, secondSession);
        Assert.Same(firstTokenProvider, secondTokenProvider);
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<AppSessionStore>(),
            secondScope.ServiceProvider.GetRequiredService<AppSessionStore>());
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<AccountTokenCache>(),
            secondScope.ServiceProvider.GetRequiredService<AccountTokenCache>());
    }

}
