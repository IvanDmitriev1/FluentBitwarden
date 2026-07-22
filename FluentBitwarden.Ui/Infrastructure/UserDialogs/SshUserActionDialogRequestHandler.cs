using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Infrastructure.UserDialogs;

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
        };

        using var cancellationRegistration = cancellationToken.Register(
            static state => _ = App.Current.DispatcherQueue.TryEnqueue(((ContentDialog)state!).Hide),
            dialog);

        try
        {
            var result = await windowManager.ShowDialogAsync(dialog, cancellationToken);
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
