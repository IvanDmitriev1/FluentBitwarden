using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.Contracts.AppSession.Unlock;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal static class AppSessionServiceTestExtensions
{
    public static SessionUnlockOutcome UnlockAsync(
        this IAppSessionService service,
        SessionUnlockRequest request) =>
        service.UnlockAsync(request, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    public static void Unlock(
        this ActiveSessionManager.Transition transition,
        UnlockedVaultLifetime vaultLifetime) =>
        transition.UnlockAsync(vaultLifetime).GetAwaiter().GetResult();

    public static void SignOut(this ActiveSessionManager.Transition transition) =>
        transition.SignOutAsync().GetAwaiter().GetResult();
}
