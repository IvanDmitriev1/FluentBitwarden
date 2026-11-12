using BitwardenApi.Client;
using BitwardenApi.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Application.Abstractions;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

public sealed partial class SetupTwoFactorPageViewModel(
    IAccountClient accountClient,
    ISetupNavigator navigator,
    IAppSession sessionRefresher) : ObservableObject, INavigationAware, INavigationAware<SetupTwoFactorParameters>
{
    private BitwardenClientContext? _context;
    private IdentityTwoFactorChallenge? _challenge;
    private string _serverAuthorizationHash = string.Empty;
    private SetupEmailParameters? _emailState;
    private CancellationToken _frameCancellationToken;
    private CancellationTokenSource? _operationCancellation;

    [ObservableProperty]
    public partial SetupTwoFactorProviderOption[] SupportedProviders { get; private set; } = [];

    [ObservableProperty]
    public partial string Email { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string ServerDisplayName { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnsupportedProvidersMessage))]
    [NotifyPropertyChangedFor(nameof(HasUnsupportedProviders))]
    public partial string UnsupportedProvidersText { get; private set; } = string.Empty;

    public string UnsupportedProvidersMessage =>
        string.IsNullOrEmpty(UnsupportedProvidersText)
            ? string.Empty
            : $"Methods unavailable in this build: {UnsupportedProvidersText}";

    public bool HasUnsupportedProviders => !string.IsNullOrEmpty(UnsupportedProvidersText);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedProvider))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial SetupTwoFactorProviderOption? SelectedProviderOption { get; set; }

    public IdentityTwoFactorProviderType SelectedProvider => SelectedProviderOption?.Provider ?? default;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsBusy { get; private set; }

    public bool CanSubmit =>
        !IsBusy &&
        SelectedProviderOption is { } selectedProvider &&
        SupportedProviders.Any(provider => provider.Provider == selectedProvider.Provider) &&
        !string.IsNullOrWhiteSpace(Code);

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedToAsync(SetupTwoFactorParameters parameters, CancellationToken cancellationToken)
    {
        _frameCancellationToken = cancellationToken;
        var challenge = parameters.Challenge.Challenge;
        _context = parameters.Context;
        _emailState = parameters.EmailState;
        _challenge = challenge;
        _serverAuthorizationHash = parameters.Challenge.ServerAuthorizationHash;
        Email = parameters.Challenge.Email;
        ServerDisplayName = parameters.EmailState.SelectedServer?.Title ?? string.Empty;

        SupportedProviders = challenge.Providers
            .Select(static option => option.Provider)
            .Where(IsSupported)
            .Distinct()
            .Select(static provider => new SetupTwoFactorProviderOption(provider, GetProviderTitle(provider)))
            .ToArray();

        UnsupportedProvidersText = string.Join(
            ", ",
            challenge.Providers
                .Select(static option => option.Provider)
                .Where(static provider => !IsSupported(provider))
                .Distinct()
                .Select(GetProviderTitle));

        Code = string.Empty;
        SelectedProviderOption = SupportedProviders.FirstOrDefault();
        Error = SupportedProviders.Length == 0
            ? "This account only has verification methods that are not supported in this build."
            : null;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        _operationCancellation?.Cancel();
        ClearChallenge();
        _context = null;
        _emailState = null;
        _serverAuthorizationHash = string.Empty;
        Email = string.Empty;
        ServerDisplayName = string.Empty;
        SupportedProviders = [];
        UnsupportedProvidersText = string.Empty;
        IsBusy = false;
        return ValueTask.CompletedTask;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (!IsBusy && _emailState is { } emailState)
            navigator.GoBackToEmail(emailState);
    }

    [RelayCommand(CanExecute = nameof(CanSubmit), AllowConcurrentExecutions = false)]
    private async Task SubmitAsync()
    {
        if (IsBusy)
            return;

        if (!TryCreateRequest(out var request, out var error))
        {
            Error = error;
            return;
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_frameCancellationToken);
        _operationCancellation = operation;
        Error = null;
        try
        {
            IsBusy = true;
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request, operation.Token);
            if (operation.IsCancellationRequested)
                return;

            switch (outcome)
            {
                case AccountAuthenticationOutcome.Success:
                    ClearChallenge();
                    await sessionRefresher.RefreshSessionAsync(operation.Token);
                    break;
                case AccountAuthenticationOutcome.InvalidCredentials invalid:
                    Error = invalid.Message;
                    break;
                default:
                    Error = "Two-step verification returned an unsupported result.";
                    break;
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (!operation.IsCancellationRequested)
                Error = "Unable to connect. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
            Code = string.Empty;
            _operationCancellation = null;
        }
    }

    private bool TryCreateRequest(
        [NotNullWhen(true)] out AccountAuthenticationRequest.TwoFactor? request,
        out string? error)
    {
        request = null;
        error = null;

        if (_challenge is null || _context is not { } context)
        {
            error = "The verification challenge has expired. Go back and try again.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            error = "Enter the verification code.";
            return false;
        }

        if (SelectedProviderOption is not { } selectedProvider ||
            SupportedProviders.All(provider => provider.Provider != selectedProvider.Provider))
        {
            error = "Choose a supported verification method.";
            return false;
        }

        request = new AccountAuthenticationRequest.TwoFactor(
            context,
            Email,
            _serverAuthorizationHash,
            new IdentityTwoFactorProof(Code, selectedProvider.Provider));
        return true;
    }

    private void ClearChallenge()
    {
        Code = string.Empty;
        _challenge = null;
        Error = null;
    }

    private static bool IsSupported(IdentityTwoFactorProviderType provider) =>
        provider is IdentityTwoFactorProviderType.Authenticator or IdentityTwoFactorProviderType.Email;

    private static string GetProviderTitle(IdentityTwoFactorProviderType provider) => provider switch
    {
        IdentityTwoFactorProviderType.Authenticator => "Authenticator app",
        IdentityTwoFactorProviderType.Email => "Email code",
        IdentityTwoFactorProviderType.Duo => "Duo",
        IdentityTwoFactorProviderType.YubiKey => "YubiKey",
        IdentityTwoFactorProviderType.U2f => "U2F",
        _ => provider.ToString(),
    };
}
