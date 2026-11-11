using FluentBitwarden.Contracts.AppSession.State;

namespace FluentBitwarden.Ui.Application.Abstractions;

public interface IAppSession
{
    AppSessionStateSnapshot? CurrentSession { get; }

    Task RefreshSessionAsync(CancellationToken cancellationToken = default);
}
