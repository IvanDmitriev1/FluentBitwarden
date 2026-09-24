using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountSessionAccessTokenProviderTests
{
    [Fact]
    public async Task Reuses_cached_tokens_across_DI_scopes()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens tokens = Tokens(accountContext, "shared");
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tokens));
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope firstScope = services.CreateScope();
        using IServiceScope secondScope = services.CreateScope();

        SessionAccessToken first = await GetToken(Provider(firstScope), accountContext, TestContext.Current.CancellationToken);
        SessionAccessToken second = await GetToken(Provider(secondScope), accountContext, TestContext.Current.CancellationToken);

        Assert.Equal(tokens.AccessToken, first);
        Assert.Equal(first, second);
        await accountService.Received(1).RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>());
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
        accountService.RefreshSessionTokens(firstAccount, Arg.Any<CancellationToken>()).Returns(Task.FromResult(firstTokens));
        accountService.RefreshSessionTokens(secondAccount, Arg.Any<CancellationToken>()).Returns(Task.FromResult(secondTokens));
        accountService.RefreshSessionTokens(europeanAccount, Arg.Any<CancellationToken>()).Returns(Task.FromResult(europeanTokens));
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IBitwardenAccessTokenProvider provider = Provider(scope);

        Assert.Equal(firstTokens.AccessToken, await GetToken(provider, firstAccount, TestContext.Current.CancellationToken));
        Assert.Equal(secondTokens.AccessToken, await GetToken(provider, secondAccount, TestContext.Current.CancellationToken));
        Assert.Equal(europeanTokens.AccessToken, await GetToken(provider, europeanAccount, TestContext.Current.CancellationToken));

        await accountService.Received(1).RefreshSessionTokens(firstAccount, Arg.Any<CancellationToken>());
        await accountService.Received(1).RefreshSessionTokens(secondAccount, Arg.Any<CancellationToken>());
        await accountService.Received(1).RefreshSessionTokens(europeanAccount, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refreshes_an_expired_cached_token_and_reuses_the_replacement()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens expiredTokens = Tokens(accountContext, "expired", DateTimeOffset.UtcNow.AddMinutes(4));
        AccountSessionTokens replacementTokens = Tokens(accountContext, "replacement");
        int refreshCount = 0;
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Interlocked.Increment(ref refreshCount) == 1 ? expiredTokens : replacementTokens));
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IBitwardenAccessTokenProvider provider = Provider(scope);

        Assert.Equal(expiredTokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(replacementTokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(replacementTokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
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
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                Interlocked.Increment(ref refreshCount);
                refreshStarted.TrySetResult();
                await WaitWithCancellationAsync(releaseRefresh.Task, call.Arg<CancellationToken>());
                return tokens;
            });
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope firstScope = services.CreateScope();
        using IServiceScope secondScope = services.CreateScope();

        Task<SessionAccessToken> first = GetToken(Provider(firstScope), accountContext, TestContext.Current.CancellationToken).AsTask();
        await WaitWithTimeoutAsync(refreshStarted.Task, TimeSpan.FromSeconds(5));
        Task<SessionAccessToken> second = GetToken(Provider(secondScope), accountContext, TestContext.Current.CancellationToken).AsTask();
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
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref refreshCount) == 1
                ? Task.FromException<AccountSessionTokens>(new InvalidOperationException("Synthetic refresh failure."))
                : Task.FromResult(tokens));
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IBitwardenAccessTokenProvider provider = Provider(scope);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(tokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(2, refreshCount);
    }

    [Fact]
    public async Task Releases_refresh_coordination_after_cancellation()
    {
        var accountService = Substitute.For<IAccountService>();
        BitwardenAccountContext accountContext = Context(AccountTestData.FirstUserId);
        AccountSessionTokens tokens = Tokens(accountContext, "after-cancellation");
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                if (Interlocked.Increment(ref refreshCount) == 1)
                {
                    refreshStarted.TrySetResult();
                    await DelayUntilCancelledAsync(call.Arg<CancellationToken>());
                }

                return tokens;
            });
        using ServiceProvider services = CreateServices(accountService);
        using IServiceScope scope = services.CreateScope();
        IBitwardenAccessTokenProvider provider = Provider(scope);
        using var cancellation = new CancellationTokenSource();

        Task<SessionAccessToken> cancelledRequest = GetToken(provider, accountContext, cancellation.Token).AsTask();
        await WaitWithTimeoutAsync(refreshStarted.Task, TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledRequest);

        Assert.Equal(tokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(2, refreshCount);
    }

    [Fact]
    public async Task Lock_completes_while_token_refresh_is_paused_and_keeps_the_session_locked_for_same_account()
    {
        var accountService = Substitute.For<IAccountService>();
        var vaultManager = Substitute.For<IVaultManager>();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        using var userKey = new UnlockedUserKey(account.UserId, [0x10, 0x20, 0x30]);
        var vault = Substitute.For<IUnlockedVault>();
        vault.UserId.Returns(account.UserId);
        accountService.GetAccount(account.UserId).Returns(account);
        accountService.UnlockKey(account.UserId, Arg.Any<AccountUnlockMethod>())
            .Returns(new AccountKeyUnlockResult.Success(userKey));
        vaultManager.Open(account, userKey).Returns(vault);

        BitwardenAccountContext accountContext = Context(account.UserId.ToString());
        AccountSessionTokens tokens = Tokens(accountContext, "paused-refresh");
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        accountService.RefreshSessionTokens(accountContext, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                refreshStarted.TrySetResult();
                await WaitWithCancellationAsync(releaseRefresh.Task, call.Arg<CancellationToken>());
                return tokens;
            });

        using ServiceProvider services = CreateServices(accountService, vaultManager);
        using IServiceScope scope = services.CreateScope();
        IAppSessionService sessionService = scope.ServiceProvider.GetRequiredService<IAppSessionService>();
        Assert.IsType<SessionUnlockOutcome.Success>(sessionService.Unlock(
            new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-password")));
        IBitwardenAccessTokenProvider provider = Provider(scope);
        Task<SessionAccessToken> refresh = GetToken(provider, accountContext, TestContext.Current.CancellationToken).AsTask();
        await WaitWithTimeoutAsync(refreshStarted.Task, TimeSpan.FromSeconds(5));

        Task lockTask = sessionService.LockAsync(TestContext.Current.CancellationToken);
        Task winner = await Task.WhenAny(lockTask, DelayAsync(TimeSpan.FromMilliseconds(500)));
        bool lockFinishedBeforeRefresh = winner == lockTask;
        releaseRefresh.TrySetResult();
        await refresh;
        await lockTask;

        Assert.True(lockFinishedBeforeRefresh, "Locking waited for the token refresh operation.");
        AppSessionState.Locked lockedState = Assert.IsType<AppSessionState.Locked>(sessionService.State);
        Assert.Equal(account, lockedState.Account);
        Assert.Equal(tokens.AccessToken, await GetToken(provider, accountContext, TestContext.Current.CancellationToken));
        Assert.Equal(lockedState, sessionService.State);
    }

    [Fact]
    public void Registers_the_session_service_and_token_cache_with_the_required_lifetimes()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IAccountService>());
        services.AddSingleton(Substitute.For<IVaultManager>());
        services.AddAppSessionModule();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        IAppSessionService firstSession = firstScope.ServiceProvider.GetRequiredService<IAppSessionService>();
        IAppSessionService secondSession = secondScope.ServiceProvider.GetRequiredService<IAppSessionService>();
        IBitwardenAccessTokenProvider firstTokenProvider = Provider(firstScope);
        IBitwardenAccessTokenProvider secondTokenProvider = Provider(secondScope);

        Assert.IsType<AppSessionService>(firstSession);
        Assert.NotSame(firstSession, secondSession);
        Assert.NotSame(firstTokenProvider, secondTokenProvider);
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<ActiveSessionManager>(),
            secondScope.ServiceProvider.GetRequiredService<ActiveSessionManager>());
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<AccountTokenCache>(),
            secondScope.ServiceProvider.GetRequiredService<AccountTokenCache>());
    }

    private static ServiceProvider CreateServices(IAccountService accountService, IVaultManager? vaultManager = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => accountService);
        services.AddScoped(_ => vaultManager ?? Substitute.For<IVaultManager>());
        return services.BuildServiceProvider();
    }

    private static IBitwardenAccessTokenProvider Provider(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IBitwardenAccessTokenProvider>();

    private static ValueTask<SessionAccessToken> GetToken(
        IBitwardenAccessTokenProvider provider,
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken) =>
        provider.GetAccessTokenAsync(accountContext, cancellationToken);

    private static Task WaitWithTimeoutAsync(Task task, TimeSpan timeout) =>
        task.WaitAsync(timeout, TestContext.Current.CancellationToken);

    private static Task WaitWithCancellationAsync(Task task, CancellationToken cancellationToken) =>
        task.WaitAsync(cancellationToken);

    private static Task DelayAsync(TimeSpan delay) =>
        Task.Delay(delay, TestContext.Current.CancellationToken);

    private static Task DelayUntilCancelledAsync(CancellationToken cancellationToken) =>
        Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

    private static BitwardenAccountContext Context(string userId, BitwardenEnvironment? environment = null) =>
        new(UserId.Parse(userId), environment ?? BitwardenEnvironment.UnitedStates);

    private static AccountSessionTokens Tokens(
        BitwardenAccountContext accountContext,
        string suffix,
        DateTimeOffset? expiresAt = null) =>
        new(
            accountContext.UserId,
            new BitwardenClientContext(accountContext.Environment, default!),
            SessionRefreshToken.Parse($"synthetic-refresh-{suffix}"),
            SessionAccessToken.Parse($"synthetic-access-{suffix}"),
            expiresAt ?? DateTimeOffset.UtcNow.AddHours(1));
}
