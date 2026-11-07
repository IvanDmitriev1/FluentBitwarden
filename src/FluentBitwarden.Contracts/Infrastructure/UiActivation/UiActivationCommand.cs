namespace FluentBitwarden.Contracts.Infrastructure.UiActivation;

public abstract record UiActivationCommand
{
    private UiActivationCommand() { }

    
    public sealed record OpenCommand : UiActivationCommand
    {
        public const string Argument = "";
    }

    public sealed record OverlayCommand : UiActivationCommand
    {
        public const string Argument = "--overlay";
    }

    public sealed record OpenItemCommand(CipherId Id) : UiActivationCommand
    {
        public const string Option = "--open-item";

        public override string ToString() => $"{Option} {Id}";
    }


    public static UiActivationCommand Parse(string[] arguments) => arguments switch
    {
        [] => new OpenCommand(),

        [OverlayCommand.Argument] => new OverlayCommand(),

        [OpenItemCommand.Option, var id]
            when CipherId.TryParse(id, null, out var cipherId)
            => new OpenItemCommand(cipherId),

        _ => new OpenCommand()
    };
}
