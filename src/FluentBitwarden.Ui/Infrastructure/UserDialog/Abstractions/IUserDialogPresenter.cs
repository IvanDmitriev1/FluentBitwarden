using FluentBitwarden.Contracts.Infrastructure.UserDialog;

namespace FluentBitwarden.Ui.Infrastructure.UserDialog.Abstractions;

internal interface IUserDialogPresenter
{
    Task<UserDialogResult<TResult>> ShowAsync<TResult>(
        Func<IUserDialog<TResult>> dialogFactory,
        CancellationToken cancellationToken = default)
        where TResult : notnull;
}
