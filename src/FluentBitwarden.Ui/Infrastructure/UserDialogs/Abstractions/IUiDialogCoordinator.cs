namespace FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

internal interface IUiDialogCoordinator
{
    Task<ContentDialogResult> ShowAsync(
        Func<ContentDialog> dialogFactory,
        CancellationToken cancellationToken = default);

    Task<TResult> ShowAsync<TResult>(
        Func<IUserDialog<TResult>> dialogFactory,
        CancellationToken cancellationToken = default);
}
