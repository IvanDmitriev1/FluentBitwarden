namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionAccessor
{
    AppSessionState State { get; }
}
