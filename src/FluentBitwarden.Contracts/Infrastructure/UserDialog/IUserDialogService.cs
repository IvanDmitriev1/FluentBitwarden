namespace FluentBitwarden.Contracts.Infrastructure.UserDialog;

public interface IUserDialogService
{
    Task<UserDialogResult<TResult>> ShowAsync<TResult, TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResult : notnull
        where TRequest : IIpcRequestMessage;
}
