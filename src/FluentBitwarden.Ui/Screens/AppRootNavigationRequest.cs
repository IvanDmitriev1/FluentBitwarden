using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.Ui.Screens;

public sealed record AppRootNavigationRequest(AppSessionStateSnapshot State);
