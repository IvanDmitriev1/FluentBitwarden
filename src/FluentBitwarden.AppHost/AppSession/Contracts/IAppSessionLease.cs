namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionLease : IAsyncDisposable
{
    AppSessionState AppSessionState { get; }
    CancellationToken Revoked { get; }
}
