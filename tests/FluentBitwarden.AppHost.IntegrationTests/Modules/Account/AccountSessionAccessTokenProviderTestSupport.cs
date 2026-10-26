
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

internal static class AccountSessionAccessTokenProviderTestSupport
{
    internal static ServiceProvider CreateServices(IAccountService accountService, IVaultService? vaultService = null)
    {
        var services = new ServiceCollection();
        services.AddAccountModule();
        services.AddSingleton(Substitute.For<IIpcEventPublisher>());
        services.AddScoped(_ => accountService);
        services.AddScoped(_ => vaultService ?? Substitute.For<IVaultService>());
        services.AddSingleton(Substitute.For<IAppHostTermination>());
        services.AddAppSessionModule();
        return services.BuildServiceProvider();
    }

    internal static IAccessTokenProvider Provider(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();

    internal static ValueTask<SessionAccessToken> GetToken(
        IAccessTokenProvider provider,
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken) =>
        provider.GetAccessTokenAsync(accountContext, cancellationToken);

    internal static Task WaitWithTimeoutAsync(Task task, TimeSpan timeout) =>
        task.WaitAsync(timeout, TestContext.Current.CancellationToken);

    internal static Task WaitWithCancellationAsync(Task task, CancellationToken cancellationToken) =>
        task.WaitAsync(cancellationToken);

    internal static Task DelayUntilCancelledAsync(CancellationToken cancellationToken) =>
        Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

    internal static BitwardenAccountContext Context(string userId, BitwardenEnvironment? environment = null) =>
        new(UserId.Parse(userId), environment ?? BitwardenEnvironment.UnitedStates);

    internal static AccountSessionTokens Tokens(
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
