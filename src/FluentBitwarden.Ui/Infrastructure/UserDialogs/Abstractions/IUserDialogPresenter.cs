using FluentBitwarden.Contracts.Infrastructure.UserDialog;

namespace FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

internal interface IUserDialogPresenter
{
    Task<UserDialogResult<TResult>> ShowAsync<TResult>(
        Func<IUserDialog<TResult>> dialogFactory,
        CancellationToken cancellationToken = default)
        where TResult : notnull;
}
