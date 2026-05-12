using System.Linq;
using BitwardenApi.Models;
using CommunityToolkit.WinUI;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Views.Passkey;
using WinUIEx;

namespace FluentBitwarden.Modules.Passkey.Services;

internal class PasskeyOverlayService(
    IAccountSessionManager accountSessionManager,
    IVaultService vaultService,
    IUnitOfWorkFactory unitOfWorkFactory)
    : IPasskeyOverlayService
{
    public Task<Fido2Credential> UnlockAndSelectAsync(PasskeyGetAssertionRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountProfile> accounts;

        using (var unitOfWork = unitOfWorkFactory.Create())
        {
            accounts = unitOfWork.AccountProfileRepository.GetAccounts();
        }

        return App.Current.DispatcherQueue.EnqueueAsync(() =>
            RunOverlayFlowAsync(request, accounts[0], cancellationToken));
    }

    private async Task<Fido2Credential> RunOverlayFlowAsync(
        PasskeyGetAssertionRequest request,
        AccountProfile accountProfile,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var overlayWindow = new OverlayWindow();
        await using var _ =
            cancellationToken.Register(() => overlayWindow.DispatcherQueue.TryEnqueue(overlayWindow.Close));

        overlayWindow.Closed += (_, _) => cts.Cancel();
        overlayWindow.Activate();
        overlayWindow.Show();
        overlayWindow.BringToFront();

        try
        {
            if (accountSessionManager.ActiveSession is not null)
            {
                var userKey = await ShowUnlockPageAsync(
                    overlayWindow,
                    accountProfile,
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

        var page = new OverlayUnlockPage(accountProfile, key => tcs.SetResult(key));
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

        var page = new PasskeySelectPage(new PasskeySelectPageViewModel(credential => tcs.SetResult(credential))
        {
            Items = credentials
        });

        overlayWindow.SetContent(page);
        return await tcs.Task;
    }
}
