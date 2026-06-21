namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class Icons
{
    public static IconInfo Application { get; } = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    public static IconInfo Copy { get; } = new("\uE8C8");
    public static IconInfo Unlock { get; } = new("\uE785");
}
