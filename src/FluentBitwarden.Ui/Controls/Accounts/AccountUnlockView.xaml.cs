using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Controls.Shared;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Infrastructure.Window;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Accounts;

[DependencyProperty<AccountProfile>("Account")]
[DependencyProperty<ICommand>("ResultCommand")]
public sealed partial class AccountUnlockView : UserControl
{
    private const string PermissionGlyph = "\uE8D7";
    private const string ForwardGlyph = "\uE72A";

    public AccountUnlockView()
    {
        InitializeComponent();

        _appSessionClient = App.Current.GetRequiredService<IAppSessionClient>();
        _windowsHelloAccountUnlockMethod = App.Current.GetRequiredService<IAccountWindowsHelloIntegrationClient>();
        _windowManager = App.Current.GetRequiredService<IWindowManager>();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private readonly IAppSessionClient _appSessionClient;
    private readonly IAccountWindowsHelloIntegrationClient _windowsHelloAccountUnlockMethod;
    private readonly IWindowManager _windowManager;
    private int _accountChangeVersion;

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
        AccountProfile account = Account;
        int version = Interlocked.Increment(ref _accountChangeVersion);
        WindowsHelloButton.Visibility = Visibility.Collapsed;

        AppSessionSnapshot snapshot = await _appSessionClient.GetSnapshotAsync(new());
        if (!IsCurrentAccountChange(version, account) || snapshot.CurrentAccount?.UserId != account.UserId)
            return;

        WindowsHelloEnrollmentStatus status = await _windowsHelloAccountUnlockMethod.GetEnrollmentAsync(
            new GetWindowsHelloEnrollmentRequest());

        if (!IsCurrentAccountChange(version, account))
            return;

        WindowsHelloButton.Visibility = status == WindowsHelloEnrollmentStatus.Enrolled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool IsCurrentAccountChange(int version, AccountProfile account) =>
        Volatile.Read(ref _accountChangeVersion) == version &&
        Account is { } currentAccount &&
        currentAccount.UserId == account.UserId;


    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task Unlock()
    {
        ArgumentNullException.ThrowIfNull(Account);

        if (string.IsNullOrWhiteSpace(Password))
        {
            PasswordBox.Focus(FocusState.Programmatic);
            return Task.CompletedTask;
        }

        return OnUnlockCore(new SessionUnlockRequest.MasterPasswordRequest(Account.UserId, Password));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockWithWindowsHello()
    {
        ArgumentNullException.ThrowIfNull(Account);
        return OnUnlockCore(new SessionUnlockRequest.WindowsHelloRequest(
            Account.UserId,
            new NativeWindowHandle(_windowManager.WindowHandle.ToInt64())));
    }

    private async Task OnUnlockCore(SessionUnlockRequest request)
    {
        PasswordBox.IsPasswordRevealed = false;
        PasswordBox.IsEnabled = false;
        WindowsHelloButton.IsEnabled = false;
        SessionUnlockOutcome result;

        try
        {
            result = await _appSessionClient.UnlockAsync(request);
        }
        finally
        {
            PasswordBox.IsEnabled = true;
            WindowsHelloButton.IsEnabled = true;
        }

        switch (result)
        {
            case SessionUnlockOutcome.Failure failure:
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
