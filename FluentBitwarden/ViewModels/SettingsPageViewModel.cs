using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;
using System.ComponentModel;

namespace FluentBitwarden.ViewModels;

public partial class SettingsPageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IAuthService _authService;
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;
    private readonly IPinUnlockService _pinUnlockService;
    private readonly INavigationService _navigationService;
    private readonly PageOperationState _operationState = new();
    private StoredSessionInfo? _session;

    public SettingsPageViewModel(
        IAuthService authService,
        IWindowsHelloUnlockService windowsHelloUnlockService,
        IPinUnlockService pinUnlockService,
        INavigationService navigationService)
    {
        _authService = authService;
        _windowsHelloUnlockService = windowsHelloUnlockService;
        _pinUnlockService = pinUnlockService;
        _navigationService = navigationService;
        _operationState.PropertyChanged += OnOperationStatePropertyChanged;
    }

    public bool IsBusy => _operationState.IsBusy;
    public bool HasError => _operationState.HasError;
    public string ErrorMessage => _operationState.ErrorMessage;

    [ObservableProperty]
    public partial LoginSessionDisplay SessionDisplay { get; set; } = LoginSessionDisplay.Empty;

    [ObservableProperty]
    public partial bool IsWindowsHelloConfigured { get; set; }

    [ObservableProperty]
    public partial bool CanEnableWindowsHello { get; set; }

    [ObservableProperty]
    public partial bool IsPinConfigured { get; set; }

    [ObservableProperty]
    public partial bool CanEnablePin { get; set; }

    [ObservableProperty]
    public partial string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPin { get; set; } = string.Empty;

    public string WindowsHelloStatusText => IsWindowsHelloConfigured
        ? "Windows Hello is enabled for vault unlock."
        : CanEnableWindowsHello
            ? "Windows Hello is available and can be enabled."
            : "Windows Hello is not configured on this device.";

    public string PinStatusText => IsPinConfigured
        ? "PIN unlock is enabled."
        : "Set an app PIN to unlock the vault faster.";

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        _operationState.Reset();

        StoredSessionInfo? session = await _authService.GetStoredSessionAsync(cancellationToken);
        if (session is null)
        {
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
            return;
        }

        if (session.IsLocked)
        {
            _navigationService.Navigate<LoginPage>(clearBackStack: true);
            return;
        }

        _session = session;
        SessionDisplay = new LoginSessionDisplay(
            session.Email,
            DescribeEnvironment(session));

        await RefreshAsync(cancellationToken);
    }

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
    {
        Pin = string.Empty;
        ConfirmPin = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Back()
    {
        if (!_navigationService.GoBack())
        {
            _navigationService.Navigate<VaultPage>(clearBackStack: true);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task EnableWindowsHelloAsync()
    {
        StoredSessionInfo session = RequireSession();
        _operationState.ClearStatus();

        try
        {
            await _operationState.RunBusyAsync(async () =>
            {
                await _windowsHelloUnlockService.SetupAsync(session);
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            _operationState.ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DisableWindowsHelloAsync()
    {
        StoredSessionInfo session = RequireSession();
        _operationState.ClearStatus();

        try
        {
            await _operationState.RunBusyAsync(async () =>
            {
                await _windowsHelloUnlockService.DisableAsync(session);
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            _operationState.ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task EnablePinAsync()
    {
        StoredSessionInfo session = RequireSession();
        _operationState.ClearStatus();

        if (string.IsNullOrWhiteSpace(Pin) || string.IsNullOrWhiteSpace(ConfirmPin))
        {
            _operationState.ShowError("Enter and confirm your new PIN.");
            return;
        }

        if (!string.Equals(Pin, ConfirmPin, StringComparison.Ordinal))
        {
            _operationState.ShowError("PIN confirmation does not match.");
            return;
        }

        try
        {
            string normalizedPin = Pin.Trim();

            await _operationState.RunBusyAsync(async () =>
            {
                await _pinUnlockService.SetupAsync(session, normalizedPin);
                Pin = string.Empty;
                ConfirmPin = string.Empty;
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            _operationState.ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DisablePinAsync()
    {
        StoredSessionInfo session = RequireSession();
        _operationState.ClearStatus();

        try
        {
            await _operationState.RunBusyAsync(async () =>
            {
                await _pinUnlockService.DisableAsync(session);
                Pin = string.Empty;
                ConfirmPin = string.Empty;
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            _operationState.ShowError(ex);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        StoredSessionInfo session = RequireSession();

        bool windowsHelloConfigured = await _windowsHelloUnlockService.IsConfiguredAsync(session, cancellationToken);
        bool windowsHelloSupported = await _windowsHelloUnlockService.CanSetupAsync(cancellationToken);
        bool pinConfigured = await _pinUnlockService.IsConfiguredAsync(session, cancellationToken);

        IsWindowsHelloConfigured = windowsHelloConfigured;
        CanEnableWindowsHello = windowsHelloSupported && !windowsHelloConfigured;
        IsPinConfigured = pinConfigured;
        CanEnablePin = !pinConfigured;

        OnPropertyChanged(nameof(WindowsHelloStatusText));
        OnPropertyChanged(nameof(PinStatusText));
    }

    private StoredSessionInfo RequireSession()
        => _session ?? throw new InvalidOperationException("No unlocked session is available.");

    private static string DescribeEnvironment(StoredSessionInfo session)
    {
        string host = session.Environment.ApiBase.Host;

        if (host.Contains("bitwarden.eu", StringComparison.OrdinalIgnoreCase))
        {
            return "Bitwarden EU";
        }

        if (host.Contains("bitwarden.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Bitwarden US";
        }

        return host;
    }

    private void OnOperationStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(e.PropertyName);
}
