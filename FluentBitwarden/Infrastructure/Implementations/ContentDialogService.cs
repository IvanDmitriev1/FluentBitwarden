using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Abstractions.Dialog;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Resources.Dialogs;
using FluentBitwarden.Views.Shell.Overlay;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Implementations;

internal sealed class ContentDialogService(IWindowManager windowManager) : IContentDialogService
{
    public Task<UserActionDialogOutcome> ShowUserActionAsync(object viewModel, ContentDialogOptions options) =>
        App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            try
            {
                if (!TryFindResource(options.DataTemplateKey, out var template))
                    throw new InvalidOperationException(
                        $"DataTemplate with key '{options.DataTemplateKey}' not found.");

                var dialog = new UserActionContentDialog(viewModel, template)
                {
                    Title = options.Title,
                    PrimaryButtonText = options.PrimaryButtonText,
                    SecondaryButtonText = options.SecondaryButtonText,
                    DefaultButton = options.DefaultButton,
                    XamlRoot = windowManager.GetActiveXamlRoot()
                };

                return await dialog.ShowAsync();
            }
            finally
            {
                if (windowManager.ActiveWindow is OverlayWindow)
                {
                    windowManager.CloseWindow();
                }
            }
        }, DispatcherQueuePriority.High);


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
