namespace FluentBitwarden.Ui.Dialogs.Abstractions;

public interface IContentDialog<T, TArgs, TResult>
    where TResult : notnull
{
    static abstract T Create(TArgs args);

    TResult Result { get; }
}
