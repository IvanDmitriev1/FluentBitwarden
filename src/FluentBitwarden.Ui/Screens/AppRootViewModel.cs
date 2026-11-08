using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;

namespace FluentBitwarden.Ui.Screens;

public sealed partial class AppRootViewModel : ObservableObject
{
    [ObservableProperty]
    public partial object Content { get; private set; } = new LoadingViewModel();

    public void ApplySession(AppSessionStateSnapshot session) => Content = session switch
    {
        AppSessionStateSnapshot.NotAuthenticated => new SetupViewModel(),
        AppSessionStateSnapshot.Locked locked => Content = new UnlockViewModel(locked.Account),
        AppSessionStateSnapshot.Unlocked => new ShellViewModel(),
        _ => throw new ArgumentOutOfRangeException(nameof(session))
    };
}
