namespace FluentBitwarden.AppHost.AppSession.Contracts;

public interface IAppSessionLeaseProvider
{
    IAppSessionAccessor CreateLease();

    ValueTask<IAppSessionAccessor> WaitUntilUnlockedAsync(CancellationToken cancellationToken);
}
