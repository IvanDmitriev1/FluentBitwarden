namespace FluentBitwarden.Contracts.Infrastructure.Settings.Models;

public sealed record SettingKey<T>(string Name, T DefaultValue) where T : notnull;