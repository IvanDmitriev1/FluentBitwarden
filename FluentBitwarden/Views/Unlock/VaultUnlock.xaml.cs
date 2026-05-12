using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Resources.Controls;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using FluentBitwarden.Modules.Session.Services;

namespace FluentBitwarden.Views.Unlock;

[DependencyProperty<AccountProfile>("Account")]
[DependencyProperty<ICommand>("ResultCommand")]
public sealed partial class VaultUnlock : UserControl
{
    private const string PermissionGlyph = "\uE8D7";
    private const string ForwardGlyph = "\uE72A";

    public VaultUnlock()
    {
        InitializeComponent();

        _accountSessionManager = App.Current.GetRequiredService<IAccountSessionManager>();
        _windowsHelloAccountUnlockMethod = App.Current.GetRequiredService<WindowsHelloAccountUnlockMethod>();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private readonly IAccountSessionManager _accountSessionManager;
    private readonly WindowsHelloAccountUnlockMethod _windowsHelloAccountUnlockMethod;

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

    partial void OnAccountChanged()
    {
        ArgumentNullException.ThrowIfNull(Account);

        WindowsHelloButton.Visibility = _windowsHelloAccountUnlockMethod.IsEnabled(Account.UserId)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }


    [RelayCommand]
    private void Unlock()
    {
        ArgumentNullException.ThrowIfNull(Account);

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordBox.Focus(FocusState.Programmatic);
            return;
        }

        OnUnlockCore(new AccountUnlockRequest.MasterPasswordRequest(Account, Password));
    }

    [RelayCommand]
    private void UnlockWithWindowsHello()
    {
        ArgumentNullException.ThrowIfNull(Account);
        OnUnlockCore(new AccountUnlockRequest.WindowsHelloRequest(Account));
    }

    private void OnUnlockCore(AccountUnlockRequest request)
    {
        PasswordBox.IsPasswordRevealed = false;
        PasswordBox.IsEnabled = false;
        AccountUnlockOutcome result;

        try
        {
            result = _accountSessionManager.Unlock(request);
        }
        finally
        {
            PasswordBox.IsEnabled = true;
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