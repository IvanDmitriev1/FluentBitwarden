using BitwardenApi.Modules.Notifications.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : Page
{
    private readonly CancellationTokenSource _cts = new();
    private readonly INotificationsClient _notificationsClient;
    private readonly ICurrentSessionAccessor _currentSessionAccessor;

    public ShellPage(INotificationsClient notificationsClient, ICurrentSessionAccessor currentSessionAccessor)
    {
        _notificationsClient = notificationsClient;
        _currentSessionAccessor = currentSessionAccessor;

        InitializeComponent();
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        NavigationViewItem navItem = (NavigationViewItem)args.SelectedItem;
        Type navType = navItem.Tag switch
        {
            "vault" => typeof(VaultPage),
            _ => typeof(VaultPage)
        };

        ContentFrame.Navigate(navType);
    }

    private async void ShellPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _notificationsClient.ConnectAsync(_currentSessionAccessor.CurrentContext.Environment, _cts.Token);
    }

    private async void ShellPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        _cts.Dispose();

        await _notificationsClient.DisconnectAsync();
    }
}