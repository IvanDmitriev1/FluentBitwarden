namespace FluentBitwarden.Infrastructure.UiCommand;


public abstract record UiCliCommand
{
    private UiCliCommand() { }

    public sealed record OpenCommand : UiCliCommand;
    public sealed record ExitCommand : UiCliCommand;
    public sealed record OverlayCommand : UiCliCommand;
    public sealed record OpenItemCommand(CipherId Id) : UiCliCommand;

}
