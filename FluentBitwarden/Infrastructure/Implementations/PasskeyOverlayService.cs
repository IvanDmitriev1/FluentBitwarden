using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Models;
using CommunityToolkit.WinUI;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Models;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Views.Passkey;

namespace FluentBitwarden.Infrastructure.Implementations;

internal class PasskeyOverlayService(
    IAccountSessionManager accountSessionManager,
    IVaultService vaultService,
    IUnitOfWorkFactory unitOfWorkFactory)
    : IPasskeyOverlayService
{
    public Task<Fido2Credential> UnlockAndSelectAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        if (CanUseCredentialWithoutPrompt(request, out var credential))
        {
            return Task.FromResult(credential);
        }
        return App.Current.DispatcherQueue.EnqueueAsync(() => RunOverlayFlowAsync(request, cancellationToken));
    }

    private bool CanUseCredentialWithoutPrompt(
        PasskeyGetAssertionRequest request,
        [NotNullWhen(true)] out Fido2Credential? credential)
    {
        credential = null;

        if (accountSessionManager.ActiveSession is null)
            return false;

        var userVerificationPolicy = SettingsStore.Instance.Get(AppSettingKeys.Passkeys.UserVerificationPolicyKey);
        if (userVerificationPolicy != SensitiveActionPolicy.AllowWhenUnlocked)
            return false;

        var credentials = vaultService.GetFido2Credentials(request.RpId);
        if (credentials.Count != 1)
            return false;

        credential = credentials[0];
        return true;
    }

    private async Task<Fido2Credential> RunOverlayFlowAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var overlayWindow = new OverlayWindow();
        await using var _ =
            cancellationToken.Register(() => overlayWindow.DispatcherQueue.TryEnqueue(overlayWindow.Close));

        overlayWindow.Closed += (_, _) => cts.Cancel();
        overlayWindow.ShowWindow();

        try
        {
            if (accountSessionManager.ActiveSession is null)
            {
                using var unitOfWork = unitOfWorkFactory.Create();
                var accounts = unitOfWork.AccountProfileRepository.GetAccounts();

                var userKey = await ShowUnlockPageAsync(
                    overlayWindow,
                    accounts[0],
                    cts.Token);

                vaultService.LoadLocalVault();
            }

            var credentials = vaultService.GetFido2Credentials(request.RpId);
            return await ShowCredentialSelectPageAsync(overlayWindow, credentials, cts.Token);
        }
        finally
        {
            overlayWindow.Close();
        }
    }


    private static async Task<DecryptedUserKey> ShowUnlockPageAsync(
        OverlayWindow overlayWindow,
        AccountProfile accountProfile,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<DecryptedUserKey>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var _ = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var page = new OverlayUnlockPage(accountProfile, tcs.SetResult);
        overlayWindow.SetContent(page);

        return await tcs.Task;
    }

    private static async Task<Fido2Credential> ShowCredentialSelectPageAsync(
        OverlayWindow overlayWindow,
        IReadOnlyList<Fido2Credential> credentials,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<Fido2Credential>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var _ = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var page = new PasskeySelectPage(new PasskeySelectPageViewModel(tcs.SetResult)
        {
            Items = credentials
        });

        overlayWindow.SetContent(page);
        return await tcs.Task;
    }
}
