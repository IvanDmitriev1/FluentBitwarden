namespace FluentBitwarden.Infrastructure.UserDialogs;

internal interface IUiDialogCoordinator
{
    Task<ContentDialogResult> ShowAsync(
        ContentDialog dialog,
        CancellationToken cancellationToken = default);

    Task<TResult> ShowAsync<TResult>(
        IUserDialog<TResult> dialog,
        CancellationToken cancellationToken = default);
}
