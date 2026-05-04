using CommunityToolkit.WinUI;
using FluentBitwarden.Resources.Dialogs;
using FluentBitwarden.Resources.Dialogs.Models;
using FluentBitwarden.Shared.Services.Abstractions.Dialog;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Shared.Services.Implementations;

internal sealed class ContentDialogService : IContentDialogService
{
    public Task<UserActionDialogOutcome> ShowUserActionAsync(IContentDialogViewModel viewModel) =>
        App.Current.DispatcherQueue.EnqueueAsync(() =>
        {
            if (!TryFindResource(viewModel.DataTemplateKey, out var template))
                throw new InvalidOperationException($"DataTemplate with key '{viewModel.DataTemplateKey}' not found.");

            var dialog = new UserActionContentDialog(viewModel, template)
            {
                XamlRoot = MainWindow.Instance.XamlRoot,
            };

            MainWindow.Instance.ShowWindow();
            return dialog.ShowAsync();
        });

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