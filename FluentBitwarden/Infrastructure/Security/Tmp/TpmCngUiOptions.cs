namespace FluentBitwarden.Infrastructure.Security.Tmp;

public sealed class TpmCngUiOptions
{
    public required string FriendlyName { get; init; }
    public required string Description { get; init; }
    public required string UseContext { get; init; }
    public required string CreationTitle { get; init; }
}