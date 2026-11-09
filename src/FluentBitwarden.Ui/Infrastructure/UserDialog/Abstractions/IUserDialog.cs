namespace FluentBitwarden.Ui.Infrastructure.UserDialog.Abstractions;

internal interface IUserDialog<TResult>
{
    bool TryGetResult([MaybeNullWhen(false)] out TResult result);
}
