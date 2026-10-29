namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionAccessor : IDisposable
{
    AppSessionState AppSessionState { get; }
    CancellationToken Revoked { get; }
}
