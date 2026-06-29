using FluentBitwarden.Contracts.Modules.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Controls.Passkeys;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Platform.Ipc.Abstractions;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class PasskeyCredentialSelectionRequestHandler(
    IVaultClient vaultClient,
    UiDialogDispatcher dialogDispatcher,
    IWindowManager windowManager) : IPasskeyCredentialSelectionClient, IIpcRequestsHandler
{
    public async ValueTask<Fido2Credential> SelectPasskeyCredentialAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        var credentials = await GetCredentialsAsync(request, cancellationToken);
        return await dialogDispatcher.EnqueueAsync(
            () => ShowPasskeySelectionDialogAsync(credentials, cancellationToken));
    }

    private async Task<Fido2Credential[]> GetCredentialsAsync(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        VaultCipherQuery loginCipherQuery = new() { CipherType = VaultCipherType.Login };

        var ciphers = await vaultClient.SearchCiphersAsync(loginCipherQuery, cancellationToken);
        return ciphers
            .OfType<LoginVaultCipher>()
            .SelectMany(static cipher => cipher.Fido2Credentials)
            .Where(credential => string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private async Task<Fido2Credential> ShowPasskeySelectionDialogAsync(
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
