namespace FluentBitwarden.AppHost.Infrastructure.Data.Mapping;

/// <summary>
/// Shared conversions between SQLite storage representations and domain types.
/// Centralizes the three idioms repeated across persistence mappers: unix-millisecond
/// timestamps, integer booleans, and nullable strongly-typed id columns.
/// </summary>
internal static class SqliteConversions
{
    // Unix milliseconds <-> DateTimeOffset

    public static DateTimeOffset ToDateTimeOffsetFromUnixMs(this long unixMs) =>
        DateTimeOffset.FromUnixTimeMilliseconds(unixMs);

    public static DateTimeOffset? ToDateTimeOffsetFromUnixMs(this long? unixMs) =>
        unixMs is { } value ? DateTimeOffset.FromUnixTimeMilliseconds(value) : null;

    public static long ToUnixMs(this DateTimeOffset value) =>
        value.ToUnixTimeMilliseconds();

    public static long? ToUnixMs(this DateTimeOffset? value) =>
        value?.ToUnixTimeMilliseconds();

    // SQLite integer boolean <-> bool

    public static bool ToBool(this int value) => value != 0;

    public static int ToSqliteInt(this bool value) => value ? 1 : 0;

    // Nullable strongly-typed id column <-> domain id

    /// <summary>
    /// Parses a nullable id column into a strongly-typed id, falling back to the
    /// type's empty sentinel when the column is null. Pass the id type's
    /// <c>Parse</c> method group and <c>Empty</c> value, e.g.
    /// <c>ParseOrEmpty(row.OrganizationId, OrganizationId.Parse, OrganizationId.Empty)</c>.
    /// </summary>
    public static T ParseOrEmpty<T>(string? value, Func<string, T> parse, T empty) =>
        value is null ? empty : parse(value);
}
