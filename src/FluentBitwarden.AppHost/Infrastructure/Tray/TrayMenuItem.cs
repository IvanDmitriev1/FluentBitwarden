namespace FluentBitwarden.AppHost.Infrastructure.Tray;

public abstract record TrayMenuItem
{
    private TrayMenuItem() { }

    public sealed record Command(string Text, Action Execute) : TrayMenuItem;

    public sealed record Separator : TrayMenuItem;
}
