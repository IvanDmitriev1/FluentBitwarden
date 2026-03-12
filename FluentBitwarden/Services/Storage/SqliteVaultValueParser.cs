namespace FluentBitwarden.Services.Storage;

internal static class SqliteVaultValueParser
{
    public static DateTimeOffset? ParseNullableDate(string? value, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Stored SQLite value '{columnName}' could not be parsed as DateTimeOffset.");
    }

    public static DateTimeOffset ParseRequiredDate(string value, string columnName)
    {
        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Stored SQLite value '{columnName}' could not be parsed as DateTimeOffset.");
    }
}
