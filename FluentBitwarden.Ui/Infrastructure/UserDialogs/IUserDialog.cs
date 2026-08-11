namespace FluentBitwarden.Infrastructure.UserDialogs;

public interface IUserDialog<out TResult>
{
    TResult Result { get; }
}
