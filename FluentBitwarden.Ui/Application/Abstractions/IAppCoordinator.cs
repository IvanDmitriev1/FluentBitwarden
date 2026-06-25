using FluentBitwarden.Application.Models;
using FluentBitwarden.Infrastructure.UiCommand;

namespace FluentBitwarden.Application.Abstractions;

public interface IAppCoordinator
{
    AppSessionState SessionState { get; }

    Task HandleActivation(UiCliCommand command);
    Task RefreshSessionAsync();

    Task BeginSignIn();
}
