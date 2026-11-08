namespace FluentBitwarden.Ui.Features.Settings.Converters;

public interface IOptionItem
{
    string Title { get; }
}

public interface IOptionItem<out T> : IOptionItem
{
    T Value { get; }
}
