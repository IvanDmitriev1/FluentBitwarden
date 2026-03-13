using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Controls;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Login;
using FluentBitwarden.Views.Setup;
using FluentBitwarden.Views.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.ViewModels;

public partial class SettingsPageViewModel : ObservableValidator, IPageLifecycleAware
{
    private readonly IVaultService _vaultService;
    private readonly ILocalUnlockStatusService _localUnlockStatusService;
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;
    private readonly IPinUnlockService _pinUnlockService;
    private readonly INavigationService _navigationService;
    private StoredSessionInfo? _session;
    private ValidatableProperty? _pinValidation;
    private ValidatableProperty? _confirmPinValidation;

    public SettingsPageViewModel(
        IVaultService vaultService,
        IServiceProvider serviceProvider,
        INavigationService navigationService)
    {
        _vaultService = vaultService;
        _localUnlockStatusService = serviceProvider.GetRequiredService<ILocalUnlockStatusService>();
        _windowsHelloUnlockService = serviceProvider.GetRequiredService<IWindowsHelloUnlockService>();
        _pinUnlockService = serviceProvider.GetRequiredService<IPinUnlockService>();
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
    public partial bool HasLocalUnlockData { get; set; }

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

    public string WindowsHelloStatusText => HasLocalUnlockData switch
    {
        false => "Lock and unlock once with your master password before enabling Windows Hello.",
        true when IsWindowsHelloConfigured => "Windows Hello is enabled for vault unlock.",
        true when CanEnableWindowsHello => "Windows Hello is available and can be enabled.",
        _ => "Windows Hello is not configured on this device.",
    };

    public string PinStatusText => HasLocalUnlockData switch
    {
        false => "Lock and unlock once with your master password before enabling PIN unlock.",
        true when IsPinConfigured => "PIN unlock is enabled.",
        _ => "Set an app PIN to unlock the vault faster.",
    };

    public ValidatableProperty PinValidation
        => _pinValidation ??= ValidatableProperty.Create(this, static viewModel => viewModel.Pin);

    public ValidatableProperty ConfirmPinValidation
        => _confirmPinValidation ??= ValidatableProperty.Create(this, static viewModel => viewModel.ConfirmPin);

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        ResetStatus();

        VaultSessionState state = await _vaultService.GetSessionStateAsync(cancellationToken).ConfigureAwait(true);
        switch (state)
        {
            case VaultSessionState.NoSession:
                _navigationService.Navigate<SetupPage>(clearBackStack: true);
                return;

            case VaultSessionState.Locked:
                _navigationService.Navigate<LoginPage>(clearBackStack: true);
                return;

            case VaultSessionState.Unlocked unlocked:
                _session = unlocked.Session;
                break;

            default:
                throw new InvalidOperationException("Unsupported vault session state.");
        }

        StoredSessionInfo session = RequireSession();
        SessionDisplay = new LoginSessionDisplay(
            session.Email,
            DescribeEnvironment(session));

        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    public Task OnUnloadingAsync()
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
                VaultConfigurationOutcome outcome = await _windowsHelloUnlockService
                    .SetupAsync(session)
                    .ConfigureAwait(true);

                await HandleConfigurationOutcomeAsync(outcome, resetPinEntry: false).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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
                VaultConfigurationOutcome outcome = await _windowsHelloUnlockService
                    .DisableAsync(session)
                    .ConfigureAwait(true);

                await HandleConfigurationOutcomeAsync(outcome, resetPinEntry: false).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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
                VaultConfigurationOutcome outcome = await _pinUnlockService
                    .SetupAsync(session, normalizedPin)
                    .ConfigureAwait(true);

                await HandleConfigurationOutcomeAsync(outcome, resetPinEntry: true).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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
                VaultConfigurationOutcome outcome = await _pinUnlockService
                    .DisableAsync(session)
                    .ConfigureAwait(true);

                await HandleConfigurationOutcomeAsync(outcome, resetPinEntry: true).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        LocalUnlockStatus status = await _localUnlockStatusService
            .GetAsync(RequireSession(), cancellationToken)
            .ConfigureAwait(true);

        HasLocalUnlockData = status.HasLocalVaultData;
        IsWindowsHelloConfigured = status.WindowsHello == UnlockMethodStatus.Configured;
        CanEnableWindowsHello = status.WindowsHello == UnlockMethodStatus.Available;
        IsPinConfigured = status.Pin == UnlockMethodStatus.Configured;
        CanEnablePin = status.Pin == UnlockMethodStatus.Available;

        if (!CanEnablePin)
        {
            ResetPinEntryState();
        }

        OnPropertyChanged(nameof(WindowsHelloStatusText));
        OnPropertyChanged(nameof(PinStatusText));
    }

    private async Task HandleConfigurationOutcomeAsync(
        VaultConfigurationOutcome outcome,
        bool resetPinEntry)
    {
        switch (outcome)
        {
            case VaultConfigurationOutcome.Success:
                if (resetPinEntry)
                {
                    ResetPinEntryState();
                }

                await RefreshAsync().ConfigureAwait(true);
                break;

            case VaultConfigurationOutcome.InvalidInput invalidInput:
                ShowError(invalidInput.Message);
                break;

            case VaultConfigurationOutcome.Unavailable unavailable:
                ShowError(unavailable.Message);
                break;

            case VaultConfigurationOutcome.Cancelled:
                break;

            default:
                throw new InvalidOperationException("Unsupported vault configuration outcome.");
        }
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

    private StoredSessionInfo RequireSession()
        => _session ?? throw new InvalidOperationException("No unlocked session is available.");

    private async Task RunBusyAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        IsBusy = true;

        try
        {
            await operation().ConfigureAwait(true);
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
