using BitwardenApi.Client;
using BitwardenApi.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Ui.Screens.Setup.Messages;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

public sealed partial class SetupTwoFactorViewModel(
    IAccountClient accountClient,
    IMessenger messenger) : ObservableRecipient(messenger),
    IRecipient<ShowSetupTwoFactorMessage>,
    IRecipient<ClearSetupTwoFactorChallengeMessage>
{
    private BitwardenClientContext _context;
    private IdentityTwoFactorChallenge? _challenge;
    private string _serverAuthorizationHash = string.Empty;

    [ObservableProperty]
    public partial SetupTwoFactorProviderOption[] SupportedProviders { get; private set; } = [];

    [ObservableProperty]
    public partial string Email { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnsupportedProvidersMessage))]
    [NotifyPropertyChangedFor(nameof(HasUnsupportedProviders))]
    public partial string UnsupportedProvidersText { get; private set; } = string.Empty;

    public string UnsupportedProvidersMessage =>
        string.IsNullOrEmpty(UnsupportedProvidersText)
            ? string.Empty
            : $"Methods unavailable in this build: {UnsupportedProvidersText}";

    public bool HasUnsupportedProviders => !string.IsNullOrEmpty(UnsupportedProvidersText);

    public void Receive(ShowSetupTwoFactorMessage message)
    {
        var challenge = message.Challenge.Challenge;
        _context = message.Context;
        _challenge = challenge;
        _serverAuthorizationHash = message.Challenge.ServerAuthorizationHash;

        Email = message.Challenge.Email;

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
    }

    public void Receive(ClearSetupTwoFactorChallengeMessage message) => ClearChallenge();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedProvider))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial SetupTwoFactorProviderOption? SelectedProviderOption { get; set; }

    public IdentityTwoFactorProviderType SelectedProvider => SelectedProviderOption?.Provider ?? default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsBusy { get; private set; }

    public bool CanSubmit =>
        !IsBusy &&
        SelectedProviderOption is { } selectedProvider &&
        SupportedProviders.Any(provider => provider.Provider == selectedProvider.Provider) &&
        !string.IsNullOrWhiteSpace(Code);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SubmitAsync()
    {
        try
        {
            if (IsBusy)
                return;

            if (!TryCreateRequest(out var request, out var error))
            {
                Error = error;
                return;
            }

            Error = null;
            await AuthenticateAsync(request);
        }
        finally
        {
            Code = string.Empty;
        }
    }

    private bool TryCreateRequest(
        [NotNullWhen(true)] out AccountAuthenticationRequest.TwoFactor? request,
        out string? error)
    {
        request = null;
        error = null;

        if (_challenge is null)
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
            _context,
            Email,
            _serverAuthorizationHash,
            new IdentityTwoFactorProof(Code, selectedProvider.Provider));

        return true;
    }

    private async Task AuthenticateAsync(AccountAuthenticationRequest request)
    {
        IsBusy = true;

        try
        {
            Messenger.Send(new SetupAuthenticationStateChangedMessage(true));
            AccountAuthenticationOutcome outcome = await accountClient.AuthenticateAsync(request);
            HandleOutcome(outcome);
        }
        catch (Exception)
        {
            Error = "Unable to connect. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
            Messenger.Send(new SetupAuthenticationStateChangedMessage(false));
        }
    }

    private void HandleOutcome(AccountAuthenticationOutcome outcome)
    {
        switch (outcome)
        {
            case AccountAuthenticationOutcome.Success:
                ClearChallenge();
                Messenger.Send(new AppRootSessionRefreshRequest());
                break;
            case AccountAuthenticationOutcome.InvalidCredentials invalid:
                Error = invalid.Message;
                break;
            default:
                Error = "Two-step verification returned an unsupported result.";
                break;
        }
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
