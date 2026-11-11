using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;

namespace FluentBitwarden.Ui.Application.Abstractions;

internal interface IAppRouter
{
    void Activate(UiActivationCommand command, AppSessionStateSnapshot? state);
    void Navigate(AppSessionStateSnapshot? state);
}
