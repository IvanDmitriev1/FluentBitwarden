using BitwardenApi.Modules.Notifications.Abstractions;
using FluentBitwarden.Modules.Connectivity.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : Page
{
    private CancellationTokenSource _cts = new();
    private readonly INotificationsClient _notificationsClient;
    private readonly ICurrentSessionAccessor _currentSessionAccessor;
    private readonly IConnectivityService _connectivityService;

    public ShellPage(
        INotificationsClient notificationsClient,
        ICurrentSessionAccessor currentSessionAccessor,
        IConnectivityService connectivityService)
    {
        _notificationsClient = notificationsClient;
        _currentSessionAccessor = currentSessionAccessor;
        _connectivityService = connectivityService;

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
        _cts = new CancellationTokenSource();
        _connectivityService.ConnectivityChanged += OnConnectivityChanged;

        if (!_connectivityService.HasInternetAccess)
            return;

        try
        {
            await _notificationsClient.ConnectAsync(
                _currentSessionAccessor.CurrentContext.Environment,
                _cts.Token);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private async void ShellPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;

        _cts.Cancel();
        _cts.Dispose();

        try
        {
            await _notificationsClient.DisconnectAsync();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        Task task = e.HasInternetAccess
            ? _notificationsClient.ConnectAsync(_currentSessionAccessor.CurrentContext.Environment,
                _cts.Token)
            : _notificationsClient.DisconnectAsync();

        try
        {
            await task;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}
