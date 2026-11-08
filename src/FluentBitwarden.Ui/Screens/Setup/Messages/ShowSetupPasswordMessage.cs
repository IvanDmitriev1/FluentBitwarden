using BitwardenApi.Client;

namespace FluentBitwarden.Ui.Screens.Setup.Messages;

public sealed record ShowSetupPasswordMessage(string Email, BitwardenClientContext Context, string ServerDisplayName);
