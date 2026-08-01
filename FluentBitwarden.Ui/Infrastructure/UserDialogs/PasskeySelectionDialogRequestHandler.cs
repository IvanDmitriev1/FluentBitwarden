using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Controls.Passkeys;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class PasskeySelectionDialogRequestHandler(
    IVaultClient vaultClient,
    UiDialogDispatcher dialogDispatcher,
    IWindowManager windowManager) : IPasskeyDialogClient, IIpcRequestsHandler
{
    public async ValueTask<Fido2Credential> ShowPasskeySelectionDialogAsync(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        var credentials = await GetCredentialsAsync(request, cancellationToken);
        return await dialogDispatcher.EnqueueAsync(
            () => ShowDialogOnUiThreadAsync(credentials, cancellationToken));
    }

    private async Task<Fido2Credential[]> GetCredentialsAsync(
        PasskeySelectCredentialRequest request,
        CancellationToken cancellationToken)
    {
        VaultCipherQuery loginCipherQuery = new() { CipherType = VaultCipherType.Login };

        var ciphers = await vaultClient.SearchCiphersAsync(loginCipherQuery, cancellationToken);
        return ciphers
            .OfType<LoginVaultCipher>()
            .Select(static cipher => cipher.Fido2Credential)
            .OfType<Fido2Credential>()
            .Where(credential => string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private async Task<Fido2Credential> ShowDialogOnUiThreadAsync(
        IReadOnlyList<Fido2Credential> credentials,
        CancellationToken cancellationToken)
    {
        var selectionView = new PasskeyCredentialSelectionView(credentials);

        var dialog = new ContentDialog
        {
            Content = selectionView,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var selectedCredentialTask = selectionView.WaitUntilSelectedAsync(cancellationToken);
        var dialogTask = windowManager.ShowDialogAsync(dialog, cancellationToken);

        await using var cancellationRegistration = cancellationToken.Register(
            static state => _ = App.Current.DispatcherQueue.TryEnqueue(((ContentDialog)state!).Hide),
            dialog);

        var completedTask = await Task.WhenAny(selectedCredentialTask, dialogTask);
        if (completedTask == selectedCredentialTask || selectedCredentialTask.IsCompleted)
        {
            var selectedCredential = await selectedCredentialTask;
            dialog.Hide();
            return selectedCredential;
        }

        throw new OperationCanceledException(
            "Passkey credential selection was canceled.",
            cancellationToken);
    }
}
