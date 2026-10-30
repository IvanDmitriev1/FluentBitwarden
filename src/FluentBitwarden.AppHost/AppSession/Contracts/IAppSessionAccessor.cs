namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionAccessor : IAsyncDisposable
{
    AppSessionState AppSessionState { get; }
    CancellationToken Revoked { get; }
}
