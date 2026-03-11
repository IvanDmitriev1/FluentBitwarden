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
    private readonly IVaultService _vaultService;
    private readonly ILocalUnlockStatusService _localUnlockStatusService;
    private readonly INavigationService _navigationService;
    private readonly MasterPasswordUnlockViewModel _masterPasswordUnlock;
    private readonly WindowsHelloUnlockViewModel _windowsHelloUnlock;
    private readonly PinUnlockViewModel _pinUnlock;

    private StoredSessionInfo? _session;
    private LocalUnlockStatus _unlockStatus = LocalUnlockStatus.Empty;
    private bool _hasAttemptedWindowsHelloAutoPrompt;

    public LoginPageViewModel(
        IVaultService vaultService,
        IServiceProvider serviceProvider,
        INavigationService navigationService)
    {
        _vaultService = vaultService;
        _localUnlockStatusService = serviceProvider.GetRequiredService<ILocalUnlockStatusService>();
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

        VaultSessionState state = await _vaultService.GetSessionStateAsync(cancellationToken).ConfigureAwait(true);
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

        StoredSessionInfo session = RequireSession();
        _unlockStatus = await _localUnlockStatusService.GetAsync(session, cancellationToken).ConfigureAwait(true);
        SessionDisplay = new LoginSessionDisplay(
            session.Email,
            DescribeEnvironment(session));

        HasInitializedLocalUnlock = _unlockStatus.HasLocalVaultData;
        RefreshUnlockMethods(selectPreferredMethod: true);

        if (!_hasAttemptedWindowsHelloAutoPrompt
            && SelectedUnlockMethod?.Method == LoginUnlockMethod.WindowsHello
            && _unlockStatus.WindowsHello == UnlockMethodStatus.Configured)
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
            ShowError(ex.Message);
        }
    }

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
        if (_session is null)
        {
            UnlockMethods = [];
            SelectedUnlockMethod = null;
            return;
        }

        LoginUnlockCapabilities capabilities = LoadUnlockCapabilities(_session, _unlockStatus);
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

    private static LoginUnlockCapabilities LoadUnlockCapabilities(
        StoredSessionInfo session,
        LocalUnlockStatus unlockStatus)
        => new(
            unlockStatus.WindowsHello == UnlockMethodStatus.Configured,
            unlockStatus.Pin == UnlockMethodStatus.Configured,
            session.CanUnlockWithMasterPassword);

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
        _unlockStatus = LocalUnlockStatus.Empty;
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

    private void ResetStatus()
    {
        IsBusy = false;
        HasError = false;
        ErrorMessage = string.Empty;
    }
}
