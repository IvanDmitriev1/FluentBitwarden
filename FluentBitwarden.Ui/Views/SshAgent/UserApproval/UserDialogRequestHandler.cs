using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Passkey.Models;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Views.Passkeys.CredentialSelection;
using FluentBitwarden.Views.Shell.Overlay;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.Views.SshAgent.UserApproval;

internal sealed class UserDialogRequestHandler(IWindowManager windowManager, IVaultClient vaultClient)
    : IUserDialogClient, IIpcRequestsHandler
{
    private static readonly UserActionDialogOptions SshDialogOptions = new(
        Title: "Approve SSH request?",
        PrimaryButtonText: "Approve",
        SecondaryButtonText: "Deny",
        DefaultButton: ContentDialogButton.Secondary,
        DataTemplateKey: "SshUserActionRequestViewModelTemplateKey");

    public async ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken)
    {
        return await ShowDialogAsync(() => ShowUserActionDialogAsync(request, SshDialogOptions));
    }

    public async ValueTask<Fido2Credential> SelectPasskeyCredential(
        PasskeyGetAssertionRequest request,
        CancellationToken cancellationToken)
    {
        var credentials = await GetCredentialsAsync();
        return await ShowDialogAsync(() => ShowPasskeySelectionDialogAsync(credentials, cancellationToken));

        async Task<Fido2Credential[]> GetCredentialsAsync()
        {
            VaultCipherQuery sshCipherQuery = new() { CipherType = VaultCipherType.Login };

            var ciphers = await vaultClient.SearchCiphersAsync(sshCipherQuery, cancellationToken);
            return ciphers
                .OfType<LoginVaultCipher>()
                .SelectMany(static cipher => cipher.Fido2Credentials)
                .Where(credential => string.Equals(credential.RpId, request.RpId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    private Task<T> ShowDialogAsync<T>(Func<Task<T>> showDialogAsync) =>
        App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            try
            {
                return await showDialogAsync.Invoke();
            }
            finally
            {
                if (windowManager.ActiveWindow is OverlayWindow)
                {
                    windowManager.CloseWindow();
                }
            }
        }, DispatcherQueuePriority.High);

    private async Task<Fido2Credential> ShowPasskeySelectionDialogAsync(
        IReadOnlyList<Fido2Credential> credentials,
        CancellationToken cancellationToken)
    {
        var viewModel = new PasskeySelectPageViewModel
        {
            Items = credentials
        };

        var dialog = CreatePasskeySelectionDialog(viewModel);
        var selectedCredentialTask = viewModel.WaitUntilSelectedAsync(cancellationToken);
        var dialogTask = dialog.ShowAsync().AsTask(cancellationToken);

        await using var _ = cancellationToken.Register(dialog.Hide);

        var completedTask = await Task.WhenAny(selectedCredentialTask, dialogTask);
        if (completedTask == selectedCredentialTask || selectedCredentialTask.IsCompleted)
        {
            var selectedCredential = await selectedCredentialTask;
            dialog.Hide();
            return selectedCredential;
        }

        throw new OperationCanceledException("Passkey credential selection was canceled.", cancellationToken);
    }

    private ContentDialog CreatePasskeySelectionDialog(PasskeySelectPageViewModel viewModel)
    {
        return new ContentDialog
        {
            Content = new PasskeySelectPage(viewModel),
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = windowManager.ActiveXamlRoot
        };
    }

    private async Task<UserActionDialogOutcome> ShowUserActionDialogAsync(
        object viewModel,
        UserActionDialogOptions options)
    {
        if (!TryFindResource(options.DataTemplateKey, out var template))
        {
            throw new InvalidOperationException(
                $"DataTemplate with key '{options.DataTemplateKey}' not found.");
        }

        var dialog = new UserActionContentDialog(viewModel, template)
        {
            Title = options.Title,
            PrimaryButtonText = options.PrimaryButtonText,
            SecondaryButtonText = options.SecondaryButtonText,
            DefaultButton = options.DefaultButton,
            XamlRoot = windowManager.ActiveXamlRoot
        };

        return await dialog.ShowAsync();
    }

    private static bool TryFindResource(string key, [MaybeNullWhen(false)] out DataTemplate dataTemplate)
    {
        if (App.Current.Resources.TryGetValue(key, out var resource) && resource is DataTemplate template)
        {
            dataTemplate = template;
            return true;
        }

        dataTemplate = null;
        return false;
    }

    private sealed record UserActionDialogOptions(
        string Title,
        string PrimaryButtonText,
        string SecondaryButtonText,
        ContentDialogButton DefaultButton,
        string DataTemplateKey);
}
