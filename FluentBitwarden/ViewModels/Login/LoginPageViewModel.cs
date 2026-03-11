using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Navigation;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.ViewModels;

public partial class LoginPageViewModel : ObservableObject, IPageLifecycleAware
{
    private StoredSessionInfo? _session;
    private VaultState? _vaultState;
    private bool _hasAttemptedWindowsHelloAutoPrompt;
    private readonly IVaultService _vaultService;
    private readonly ISessionManager _sessionManager;
    private readonly INavigationService _navigationService;
    private readonly MasterPasswordUnlockViewModel _masterPasswordUnlock;
    private readonly WindowsHelloUnlockViewModel _windowsHelloUnlock;
    private readonly PinUnlockViewModel _pinUnlock;

    public LoginPageViewModel(
        IVaultService vaultService,
        IServiceProvider serviceProvider,
        INavigationService navigationService)
    {
        _vaultService = vaultService;
        _sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
        _navigationService = navigationService;

        _masterPasswordUnlock = new MasterPasswordUnlockViewModel(this, vaultService);
        _windowsHelloUnlock = new WindowsHelloUnlockViewModel(this, serviceProvider.GetRequiredService<IWindowsHelloUnlockService>());
        _pinUnlock = new PinUnlockViewModel(this, vaultService);
    }

    [ObservableProperty]
    public partial LoginSessionDisplay SessionDisplay { get; set; } = LoginSessionDisplay.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUnlockMethod))]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnlockMethod))]
    public partial LoginUnlockMethodItem? SelectedUnlockMethod { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnlockMethodSelector))]
    public partial LoginUnlockMethodItem[] UnlockMethods { get; set; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasInitializedLocalUnlock { get; private set; }

    public LoginUnlockMethodItem CurrentUnlockMethod => SelectedUnlockMethod ?? _masterPasswordUnlock.Method;

    public bool HasUnlockMethodSelector => UnlockMethods.Length > 1;
    public bool HasSelectedUnlockMethod => SelectedUnlockMethod is not null;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        ResetPageState();

        VaultState state = await _vaultService.GetStateAsync(cancellationToken).ConfigureAwait(true);
        if (!state.HasStoredSession)
        {
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
            return;
        }

        if (!state.IsLocked)
        {
            _navigationService.Navigate<VaultPage>(clearBackStack: true);
            return;
        }

        _session = await _sessionManager.GetStoredSessionAsync(cancellationToken).ConfigureAwait(true);
        if (_session is null)
        {
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
            return;
        }

        _vaultState = state;
        SessionDisplay = new LoginSessionDisplay(
            state.Email ?? string.Empty,
            DescribeEnvironment(state));

        HasInitializedLocalUnlock = state.HasLocalUnlockData;
        RefreshUnlockMethods(selectPreferredMethod: true);

        if (!_hasAttemptedWindowsHelloAutoPrompt
            && SelectedUnlockMethod?.Method == LoginUnlockMethod.WindowsHello
            && state.CanUseWindowsHello)
        {
            _hasAttemptedWindowsHelloAutoPrompt = true;
            await _windowsHelloUnlock.TryAutoUnlockAsync(cancellationToken).ConfigureAwait(true);
        }
    }

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
    {
        _hasAttemptedWindowsHelloAutoPrompt = false;
        ResetUnlockInputs();
        ResetStatus();
        return Task.CompletedTask;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UseAnotherAccountAsync()
    {
        try
        {
            ClearStatus();
            await RunBusyAsync(async () =>
            {
                await _vaultService.LogoutAsync().ConfigureAwait(true);
            }).ConfigureAwait(true);

            ResetToLoggedOutState();
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public VaultState RequireState()
        => _vaultState ?? throw new InvalidOperationException("No stored Bitwarden session is available.");

    internal StoredSessionInfo RequireSession()
        => _session ?? throw new InvalidOperationException("No stored Bitwarden session is available.");

    public async Task RunBusyAsync(Func<Task> operation)
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

    public void ClearStatus() => ResetStatus();

    public void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }

    public void ShowError(Exception exception) => ShowError(AuthErrorMessageFormatter.Format(exception));

    internal async Task HandleUnlockOutcomeAsync(
        VaultUnlockOutcome outcome,
        bool recommendUnlockSetup = false)
    {
        switch (outcome)
        {
            case VaultUnlockOutcome.Success:
                await CompleteUnlockAsync(recommendUnlockSetup).ConfigureAwait(true);
                break;

            case VaultUnlockOutcome.InvalidCredentials invalidCredentials:
                ShowError(invalidCredentials.Message);
                break;

            case VaultUnlockOutcome.Unavailable unavailable:
                ShowError(unavailable.Message);
                break;

            case VaultUnlockOutcome.Cancelled:
                break;

            default:
                throw new InvalidOperationException("Unsupported vault unlock outcome.");
        }
    }

    public Task CompleteUnlockAsync(bool recommendUnlockSetup = false)
    {
        ResetUnlockInputs();
        _navigationService.Navigate<VaultPage>(
            recommendUnlockSetup ? new VaultNavigationContext(true) : null,
            clearBackStack: true);
        return Task.CompletedTask;
    }

    private void RefreshUnlockMethods(bool selectPreferredMethod)
    {
        if (_vaultState is null)
        {
            UnlockMethods = [];
            SelectedUnlockMethod = null;
            return;
        }

        LoginUnlockCapabilities capabilities = LoadUnlockCapabilities(_vaultState);
        ApplyCapabilities(capabilities);

        LoginUnlockMethod? previousSelection = SelectedUnlockMethod?.Method;
        LoginUnlockMethodItem[] unlockMethods = capabilities.BuildOptions(
            _windowsHelloUnlock,
            _masterPasswordUnlock,
            _pinUnlock);

        UnlockMethods = unlockMethods;

        LoginUnlockMethod? selectedMethod = selectPreferredMethod
            ? capabilities.DeterminePreferredMethod()
            : ResolveSelection(previousSelection, unlockMethods, capabilities);

        SelectedUnlockMethod = selectedMethod is null
            ? null
            : unlockMethods.FirstOrDefault(option => option.Method == selectedMethod.Value);
    }

    private static LoginUnlockCapabilities LoadUnlockCapabilities(VaultState state)
        => new(
            state.IsWindowsHelloConfigured && state.CanUseWindowsHello,
            state.IsPinConfigured,
            state.CanUnlockWithMasterPassword);

    private void ApplyCapabilities(LoginUnlockCapabilities capabilities)
    {
        _masterPasswordUnlock.SetAvailability(capabilities.MasterPasswordAvailable);
        _windowsHelloUnlock.SetAvailability(capabilities.WindowsHelloAvailable);
        _pinUnlock.SetAvailability(capabilities.PinAvailable);
    }

    private static LoginUnlockMethod? ResolveSelection(
        LoginUnlockMethod? previousSelection,
        IReadOnlyList<LoginUnlockMethodItem> unlockMethods,
        LoginUnlockCapabilities capabilities)
    {
        if (previousSelection is not null
            && unlockMethods.Any(option => option.Method == previousSelection.Value))
        {
            return previousSelection.Value;
        }

        return capabilities.DeterminePreferredMethod();
    }

    private void ResetPageState()
    {
        ResetStatus();
        ResetUnlockInputs();
        SessionDisplay = LoginSessionDisplay.Empty;
        UnlockMethods = [];
        SelectedUnlockMethod = null;
        _session = null;
        _vaultState = null;
        HasInitializedLocalUnlock = false;
    }

    private void ResetToLoggedOutState()
    {
        _hasAttemptedWindowsHelloAutoPrompt = false;
        ResetPageState();
    }

    private void ResetUnlockInputs()
    {
        _masterPasswordUnlock.Reset();
        _windowsHelloUnlock.Reset();
        _pinUnlock.Reset();
    }

    private static string DescribeEnvironment(VaultState state)
    {
        string host = state.Environment?.ApiBase.Host ?? string.Empty;

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

    private void ResetStatus()
    {
        IsBusy = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
