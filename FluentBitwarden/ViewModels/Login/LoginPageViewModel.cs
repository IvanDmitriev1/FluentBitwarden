using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Navigation;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Ui.Abstractions;

namespace FluentBitwarden.ViewModels;

public partial class LoginPageViewModel : ObservableObject, IPageLifecycleAware
{
    private StoredSessionInfo? _session;
    private bool _hasAttemptedWindowsHelloAutoPrompt;
    private readonly IAuthService _authService;
    private readonly ILocalVaultUnlocker _localVaultUnlocker;
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;
    private readonly IPinUnlockService _pinUnlockService;
    private readonly INavigationService _navigationService;
    private readonly MasterPasswordUnlockViewModel _masterPasswordUnlock;
    private readonly WindowsHelloUnlockViewModel _windowsHelloUnlock;
    private readonly PinUnlockViewModel _pinUnlock;

    public LoginPageViewModel(
        IAuthService authService,
        ILocalVaultUnlocker localVaultUnlocker,
        IMasterPasswordUnlockService masterPasswordUnlockService,
        IWindowsHelloUnlockService windowsHelloUnlockService,
        IPinUnlockService pinUnlockService,
        INavigationService navigationService)
    {
        _authService = authService;
        _localVaultUnlocker = localVaultUnlocker;
        _windowsHelloUnlockService = windowsHelloUnlockService;
        _pinUnlockService = pinUnlockService;
        _navigationService = navigationService;

        _masterPasswordUnlock = new MasterPasswordUnlockViewModel(this, masterPasswordUnlockService);
        _windowsHelloUnlock = new WindowsHelloUnlockViewModel(this, windowsHelloUnlockService);
        _pinUnlock = new PinUnlockViewModel(this, pinUnlockService);
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

    public bool HasInitializedLocalVaultUnlocker { get; private set; }

    public LoginUnlockMethodItem CurrentUnlockMethod => SelectedUnlockMethod ?? _masterPasswordUnlock.Method;

    public bool HasUnlockMethodSelector => UnlockMethods.Length > 1;
    public bool HasSelectedUnlockMethod => SelectedUnlockMethod is not null;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        ResetPageState();

        StoredSessionInfo? session = await _authService.GetStoredSessionAsync(cancellationToken);
        if (session is null)
        {
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
            return;
        }

        _session = session;
        SessionDisplay = new LoginSessionDisplay(
            session.Email,
            DescribeEnvironment(session));

        HasInitializedLocalVaultUnlocker = await _localVaultUnlocker.IsInitializedAsync(session.AccountId, cancellationToken);

        await RefreshUnlockMethodsCoreAsync(selectPreferredMethod: true, cancellationToken).ConfigureAwait(true);

        if (!_hasAttemptedWindowsHelloAutoPrompt
            && SelectedUnlockMethod?.Method == LoginUnlockMethod.WindowsHello
            && _windowsHelloUnlock.Method.IsAvailable)
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
                await _authService.LogoutAsync().ConfigureAwait(true);
                await _localVaultUnlocker.ClearAsync().ConfigureAwait(true);
            }).ConfigureAwait(true);

            ResetToLoggedOutState();
            _navigationService.Navigate<SetupPage>(clearBackStack: true);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    public StoredSessionInfo RequireSession()
        => _session ?? throw new InvalidOperationException("No stored Bitwarden session is available.");

    public async Task RunBusyAsync(Func<Task> operation)
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

    public void ClearStatus() => ResetStatus();

    public void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }

    public void ShowError(Exception exception) => ShowError(AuthErrorMessageFormatter.Format(exception));

    public Task CompleteUnlockAsync(bool recommendUnlockSetup = false)
    {
        ResetUnlockInputs();
        _navigationService.Navigate<VaultPage>(
            recommendUnlockSetup ? new VaultNavigationContext(true) : null,
            clearBackStack: true);
        return Task.CompletedTask;
    }

    private async Task RefreshUnlockMethodsCoreAsync(bool selectPreferredMethod, CancellationToken cancellationToken)
    {
        if (_session is null)
        {
            UnlockMethods = [];
            SelectedUnlockMethod = null;
            return;
        }

        LoginUnlockCapabilities capabilities = await LoadUnlockCapabilitiesAsync(_session, cancellationToken).ConfigureAwait(true);
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

    private async Task<LoginUnlockCapabilities> LoadUnlockCapabilitiesAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken)
    {
        bool windowsHelloIsAvailable = await _windowsHelloUnlockService.IsConfiguredAsync(session, cancellationToken).ConfigureAwait(true)
            && await _windowsHelloUnlockService.CanSetupAsync(cancellationToken).ConfigureAwait(true);
        bool pinIsAvailable = await _pinUnlockService.IsConfiguredAsync(session, cancellationToken).ConfigureAwait(true);

        return new LoginUnlockCapabilities(
            windowsHelloIsAvailable,
            pinIsAvailable,
            session.CanUnlockWithMasterPassword);
    }

    private void ApplyCapabilities(LoginUnlockCapabilities capabilities)
    {
        _masterPasswordUnlock.SetAvailability(capabilities.MasterPasswordAvailable);
        _windowsHelloUnlock.SetAvailability(capabilities.WindowsHelloAvailable);
        _pinUnlock.SetAvailability(capabilities.PinAvailable);
    }

    private LoginUnlockMethod? ResolveSelection(
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
        HasInitializedLocalVaultUnlocker = false;
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
