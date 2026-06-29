namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class Icons
{
    public static IconInfo Application { get; } = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
    public static IconInfo Copy { get; } = new("\uE8C8");
    public static IconInfo Unlock { get; } = new("\uE785");
    public static IconInfo Login { get; } = new("\uE774");
    public static IconInfo SecureNote { get; } = new("\uE70B");
    public static IconInfo Card { get; } = new("\uE8C7");
    public static IconInfo Identity { get; } = new("\uE77B");
    public static IconInfo SshKey { get; } = new("\uE192");
}
