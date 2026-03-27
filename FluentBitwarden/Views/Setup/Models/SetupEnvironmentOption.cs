using BitwardenApi.Context;

namespace FluentBitwarden.Views.Setup.Models;

public sealed record SetupEnvironmentOption(string Title, string Subtitle, BitwardenEnvironment Environment);