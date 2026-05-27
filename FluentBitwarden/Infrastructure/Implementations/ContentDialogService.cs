using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.WinUI;
using FluentBitwarden.Infrastructure.Abstractions.Dialog;
using FluentBitwarden.Resources.Dialogs;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Implementations;

internal sealed class ContentDialogService : IContentDialogService
{
    public Task<UserActionDialogOutcome> ShowUserActionAsync(object viewModel, ContentDialogOptions options) =>
        App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            if (!TryFindResource(options.DataTemplateKey, out var template))
                throw new InvalidOperationException($"DataTemplate with key '{options.DataTemplateKey}' not found.");

            var dialog = new UserActionContentDialog(viewModel, template)
            {
                XamlRoot = MainWindow.Instance.XamlRoot,
                Title = options.Title,
                PrimaryButtonText = options.PrimaryButtonText,
                SecondaryButtonText = options.SecondaryButtonText,
                DefaultButton = options.DefaultButton,
            };

            bool wasHidden = MainWindow.Instance.IsHidden;

            MainWindow.Instance.ShowWindow();
            var result = await dialog.ShowAsync();

            if (wasHidden)
                MainWindow.Instance.Hide();

            return result;
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