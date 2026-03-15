using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Extensions;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Vault;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FluentBitwarden.ViewModels.Login;

public sealed partial class LoginPageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IVaultService _vaultService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ILocalUnlockStatusService _localUnlockStatusService;
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;

    private StoredSessionInfo? _session;

    public LoginPageViewModel(
        IVaultService vaultService,
        INavigationService navigationService,
        INotificationService notificationService,
        ILocalUnlockStatusService localUnlockStatusService,
        IWindowsHelloUnlockService windowsHelloUnlockService)
    {
        _vaultService = vaultService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _localUnlockStatusService = localUnlockStatusService;
        _windowsHelloUnlockService = windowsHelloUnlockService;

        SelectedUnlockMethod = new MasterPasswordUnlockViewModel(this, vaultService);
        UnlockMethods = [SelectedUnlockMethod];
    }

    [ObservableProperty]
    public partial LoginSessionDisplay SessionDisplay { get; set; } = LoginSessionDisplay.Empty;

    public ObservableCollection<ILoginUnlockMethod> UnlockMethods { get; set; }

    [ObservableProperty]
    public partial ILoginUnlockMethod SelectedUnlockMethod { get; set; }

    public bool HasUnlockMethodSelector => UnlockMethods.Count > 1;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        VaultSessionState state = await _vaultService.GetSessionStateAsync(cancellationToken);
        switch (state)
        {
            case VaultSessionState.NoSession:
                _navigationService.Navigate<SetupPage>(clearBackStack: true);
                return;

            case VaultSessionState.Unlocked:
                _navigationService.Navigate<VaultPage>(clearBackStack: true);
                return;

            case VaultSessionState.Locked locked:
                _session = locked.Session;
                break;

            default:
                throw new InvalidOperationException("Unsupported vault session state.");
        }

        SessionDisplay = new LoginSessionDisplay(
            _session.Email,
            _session.DescribeEnvironment());

        await RefreshUnlockMethods();
    }

    public Task OnUnloadingAsync()
    {
        _session = null;

        return Task.CompletedTask;
    }

    public void HandleUnlockOutcomeAsync(
        VaultUnlockOutcome outcome,
        bool recommendUnlockSetup = false)
    {
        try
        {

        }
        catch (Exception e)
        {
            App.WriteException(e);
        }

        switch (outcome)
        {
            case VaultUnlockOutcome.Success:
                _navigationService.Navigate<ShellPage>();
                break;

            case VaultUnlockOutcome.Unavailable unavailable:
                _notificationService.ShowError("Login", unavailable.Message);
                break;

            case VaultUnlockOutcome.Cancelled:
                break;

            default:
                throw new InvalidOperationException("Unsupported vault unlock outcome.");
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UseAnotherAccountAsync()
    {
        try
        {
            await _vaultService.LogoutAsync();
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Login page", ex.Message);
        }
    }

    private async Task RefreshUnlockMethods()
    {
        Debug.Assert(_session is not null);
        var unlockStatus = await _localUnlockStatusService.GetAsync(_session);

        OnPropertyChanged(nameof(HasUnlockMethodSelector));
    }
}