using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;
using System.ComponentModel.DataAnnotations;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Controls;

namespace FluentBitwarden.ViewModels;

public partial class SettingsPageViewModel : ObservableValidator, IPageLifecycleAware
{
    private readonly IAuthService _authService;
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;
    private readonly IPinUnlockService _pinUnlockService;
    private readonly INavigationService _navigationService;
    private StoredSessionInfo? _session;
    private ValidatableProperty? _pinValidation;
    private ValidatableProperty? _confirmPinValidation;

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
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

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
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter your new PIN.")]
    public partial string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Confirm your new PIN.")]
    [CustomValidation(typeof(SettingsPageViewModel), nameof(ValidateConfirmPin))]
    public partial string ConfirmPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowValidationErrors { get; set; }

    public string WindowsHelloStatusText => IsWindowsHelloConfigured
        ? "Windows Hello is enabled for vault unlock."
        : CanEnableWindowsHello
            ? "Windows Hello is available and can be enabled."
            : "Windows Hello is not configured on this device.";

    public string PinStatusText => IsPinConfigured
        ? "PIN unlock is enabled."
        : "Set an app PIN to unlock the vault faster.";

    public ValidatableProperty PinValidation
        => _pinValidation ??= ValidatableProperty.Create(this, static viewModel => viewModel.Pin);

    public ValidatableProperty ConfirmPinValidation
        => _confirmPinValidation ??= ValidatableProperty.Create(this, static viewModel => viewModel.ConfirmPin);

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        ResetStatus();

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
        ResetPinEntryState();
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
        ClearStatus();

        try
        {
            await RunBusyAsync(async () =>
            {
                await _windowsHelloUnlockService.SetupAsync(session);
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DisableWindowsHelloAsync()
    {
        StoredSessionInfo session = RequireSession();
        ClearStatus();

        try
        {
            await RunBusyAsync(async () =>
            {
                await _windowsHelloUnlockService.DisableAsync(session);
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task EnablePinAsync()
    {
        StoredSessionInfo session = RequireSession();
        ClearStatus();

        if (!TryValidateForSubmit())
        {
            return;
        }

        try
        {
            string normalizedPin = Pin.Trim();

            await RunBusyAsync(async () =>
            {
                await _pinUnlockService.SetupAsync(session, normalizedPin);
                ResetPinEntryState();
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DisablePinAsync()
    {
        StoredSessionInfo session = RequireSession();
        ClearStatus();

        try
        {
            await RunBusyAsync(async () =>
            {
                await _pinUnlockService.DisableAsync(session);
                ResetPinEntryState();
                await RefreshAsync();
            });
        }
        catch (Exception ex)
        {
            ShowError(ex);
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

        if (!CanEnablePin)
        {
            ResetPinEntryState();
        }

        OnPropertyChanged(nameof(WindowsHelloStatusText));
        OnPropertyChanged(nameof(PinStatusText));
    }

    partial void OnPinChanged(string value)
        => ValidateProperty(ConfirmPin, nameof(ConfirmPin));

    public static ValidationResult? ValidateConfirmPin(string? confirmPin, ValidationContext context)
    {
        SettingsPageViewModel viewModel = (SettingsPageViewModel)context.ObjectInstance;

        if (string.IsNullOrWhiteSpace(viewModel.Pin) || string.IsNullOrWhiteSpace(confirmPin))
        {
            return ValidationResult.Success;
        }

        return string.Equals(viewModel.Pin, confirmPin, StringComparison.Ordinal)
            ? ValidationResult.Success
            : new ValidationResult("PIN confirmation does not match.");
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

    private async Task RunBusyAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        IsBusy = true;

        try
        {
            await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearStatus()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private void ShowError(Exception exception)
        => ShowError(AuthErrorMessageFormatter.Format(exception));

    private void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }

    private void ResetPinEntryState()
    {
        Pin = string.Empty;
        ConfirmPin = string.Empty;
        ResetValidation();
    }

    private void ResetStatus()
    {
        IsBusy = false;
        ClearStatus();
    }

    public bool TryValidateForSubmit()
    {
        ShowValidationErrors = true;
        ValidateAllProperties();
        return !HasErrors;
    }

    public void ResetValidation()
    {
        ShowValidationErrors = false;
    }
}
