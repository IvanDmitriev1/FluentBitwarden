using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.UI.Controls;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

namespace FluentBitwarden.Views.Accounts.Unlock;

[DependencyProperty<AccountProfile>("Account")]
[DependencyProperty<ICommand>("ResultCommand")]
public sealed partial class VaultUnlock : UserControl
{
    private const string PermissionGlyph = "\uE8D7";
    private const string ForwardGlyph = "\uE72A";

    public VaultUnlock()
    {
        InitializeComponent();

        _accountsClient = App.Current.GetRequiredService<IAccountsClient>();
        _windowsHelloAccountUnlockMethod = App.Current.GetRequiredService<IWindowsHelloUnlockClient>();
        _windowManager = App.Current.GetRequiredService<IWindowManager>();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private readonly IAccountsClient _accountsClient;
    private readonly IWindowsHelloUnlockClient _windowsHelloAccountUnlockMethod;
    private readonly IWindowManager _windowManager;

    public string Password => PasswordBox.Password;


    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        PasswordBox.PasswordChanged += PasswordChanged;

        SyncPasswordAccentIcon();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;

        PasswordBox.PasswordChanged -= PasswordChanged;
    }

    private void PasswordChanged(PasswordBoxEx sender, string newPassword) => SyncPasswordAccentIcon();

    async partial void OnAccountChanged()
    {
        ArgumentNullException.ThrowIfNull(Account);

        var status = await _windowsHelloAccountUnlockMethod.GetStatusAsync(new GetWindowsHelloStatusRequest(Account.UserId));

        WindowsHelloButton.Visibility = status.IsEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }


    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task Unlock()
    {
        ArgumentNullException.ThrowIfNull(Account);

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordBox.Focus(FocusState.Programmatic);
            return Task.CompletedTask;
        }

        return OnUnlockCore(new AccountUnlockRequest.MasterPasswordRequest(Account, Password));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockWithWindowsHello()
    {
        ArgumentNullException.ThrowIfNull(Account);
        return OnUnlockCore(new AccountUnlockRequest.WindowsHelloRequest(Account, _windowManager.GetActiveWindowHandle()));
    }

    private async Task OnUnlockCore(AccountUnlockRequest request)
    {
        PasswordBox.IsPasswordRevealed = false;
        PasswordBox.IsEnabled = false;
        WindowsHelloButton.IsEnabled = false;
        AccountUnlockOutcome result;

        try
        {
            result = await _accountsClient.UnlockAsync(request);
        }
        finally
        {
            PasswordBox.IsEnabled = true;
            WindowsHelloButton.IsEnabled = true;
        }

        switch (result)
        {
            case AccountUnlockOutcome.Failure failure:
                InfoBar.Message = failure.Reason;
                InfoBar.IsOpen = true;
                PasswordBox.Focus(FocusState.Programmatic);
                break;
            default:
                ResultCommand?.Execute(result);
                break;
        }
    }

    private void SyncPasswordAccentIcon()
    {
        PasswordBoxActionButtonIcon.Glyph = string.IsNullOrWhiteSpace(Password) ? PermissionGlyph : ForwardGlyph;
    }
}
