using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Services.Window;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Services.UserDialogs;

internal sealed class SshUserActionDialogRequestHandler(
    UiDialogDispatcher dialogDispatcher,
    IWindowManager windowManager) : ISshUserActionDialogClient, IIpcRequestsHandler
{
    private const string TemplateKey = "SshUserActionRequestViewModelTemplateKey";

    public ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(UserActionDialogOutcome.Denied);
        }

        return new ValueTask<UserActionDialogOutcome>(
            dialogDispatcher.EnqueueAsync(() => ShowDialogOnUiThreadAsync(request, cancellationToken)));
    }

    private async Task<UserActionDialogOutcome> ShowDialogOnUiThreadAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return UserActionDialogOutcome.Denied;
        }

        if (!TryFindResource(TemplateKey, out var template))
        {
            throw new InvalidOperationException(
                $"DataTemplate with key '{TemplateKey}' not found.");
        }

        var dialog = new ContentDialog
        {
            Content = request,
            ContentTemplate = template,
            Title = "Approve SSH request?",
            PrimaryButtonText = "Approve",
            SecondaryButtonText = "Deny",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = windowManager.ActiveXamlRoot
        };

        using var cancellationRegistration = cancellationToken.Register(
            static state => _ = App.Current.DispatcherQueue.TryEnqueue(((ContentDialog)state!).Hide),
            dialog);

        try
        {
            var result = await dialog.ShowAsync().AsTask(cancellationToken);
            return result == ContentDialogResult.Primary
                ? UserActionDialogOutcome.Approved
                : UserActionDialogOutcome.Denied;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return UserActionDialogOutcome.Denied;
        }
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
}
