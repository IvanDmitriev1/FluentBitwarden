using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.Ui.Application;

public sealed partial class AppRootViewModel : ObservableObject
{
    [ObservableProperty]
    public partial object Content { get; private set; }

    public void ApplySession(AppSessionStateSnapshot session)
    {

    }
}
