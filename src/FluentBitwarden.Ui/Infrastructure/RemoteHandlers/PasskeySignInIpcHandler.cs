using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Dialogs.PasskeySignIn;
using FluentBitwarden.Ui.Infrastructure.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.RemoteHandlers;

internal sealed class PasskeySignInIpcHandler(
    IDialogService dialogService,
    WinUiHostedService<App> uiThread) : IIpcRequestsHandler
{
    public async Task<UserDialogResult<PasskeySelectCredentialResponse>> ShowPasskeySignInDialog(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || request.Candidates.Length == 0)
            return UserDialogResult<PasskeySelectCredentialResponse>.Cancelled;

        try
        {
            PasskeySelectCredentialResponse selection = await uiThread.RunOnUiThreadAsync(() => request.Candidates.Length == 1
                ? dialogService.ShowAsync<PasskeySignInDialog, PasskeySelectCredentialRequest, PasskeySelectCredentialResponse>(request, cancellationToken)
                : dialogService.ShowAsync<PasskeySignInPickerDialog, PasskeySelectCredentialRequest, PasskeySelectCredentialResponse>(request, cancellationToken));

            return cancellationToken.IsCancellationRequested || selection.CipherId.IsEmpty
                ? UserDialogResult<PasskeySelectCredentialResponse>.Cancelled
                : UserDialogResult<PasskeySelectCredentialResponse>.Completed(selection);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return UserDialogResult<PasskeySelectCredentialResponse>.Cancelled;
        }
    }
}
