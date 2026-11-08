using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Screens.Setup.Messages;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

namespace FluentBitwarden.Ui.Screens.Setup;

public sealed partial class SetupViewModel : ObservableRecipient,
    IRecipient<ShowSetupPasswordMessage>,
    IRecipient<ShowSetupTwoFactorMessage>,
    IRecipient<SetupAuthenticationStateChangedMessage>
{
    private readonly IAccountClient _accountClient;
    private readonly IWindowManager _windowManager;

    public SetupViewModel(
        IAccountClient accountClient,
        IMessenger messenger,
        IWindowManager windowManager) : base(messenger)
    {
        _accountClient = accountClient;
        _windowManager = windowManager;
        EmailStep = CreateEmailStep();
        PasswordStep = new SetupPasswordViewModel(_accountClient, Messenger);
        TwoFactorStep = new SetupTwoFactorViewModel(_accountClient, Messenger);
        CurrentStep = EmailStep;
    }

    [RequiresDynamicCode("ObservableRecipient.OnActivated uses reflection to register recipients.")]
    [RequiresUnreferencedCode("ObservableRecipient.OnActivated uses reflection to register recipients.")]
    protected override void OnActivated()
    {
        base.OnActivated();
        EmailStep.IsActive = true;
        PasswordStep.IsActive = true;
        TwoFactorStep.IsActive = true;
    }

    protected override void OnDeactivated()
    {
        EmailStep.IsActive = false;
        PasswordStep.IsActive = false;
        TwoFactorStep.IsActive = false;
        base.OnDeactivated();
    }

    public SetupEmailViewModel EmailStep { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial object CurrentStep { get; private set; }

    public SetupPasswordViewModel PasswordStep { get; }

    public SetupTwoFactorViewModel TwoFactorStep { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    public partial bool IsAuthenticationBusy { get; private set; }

    public bool CanGoBack => CurrentStep is not SetupEmailViewModel && !IsAuthenticationBusy;

    [RelayCommand]
    private void GoBack()
    {
        if (!CanGoBack)
            return;

        if (CurrentStep is SetupTwoFactorViewModel)
            Messenger.Send(new ClearSetupTwoFactorChallengeMessage());

        CurrentStep = EmailStep;
    }

    public void Receive(ShowSetupPasswordMessage message)
    {
        if (CurrentStep is not SetupEmailViewModel)
            return;

        CurrentStep = PasswordStep;
    }

    public void Receive(ShowSetupTwoFactorMessage message)
    {
        if (CurrentStep is not SetupPasswordViewModel)
            return;

        CurrentStep = TwoFactorStep;
    }

    public void Receive(SetupAuthenticationStateChangedMessage message)
    {
        IsAuthenticationBusy = message.IsBusy;
    }

    private SetupEmailViewModel CreateEmailStep() => new(
        _accountClient,
        _windowManager,
        Messenger);
}
