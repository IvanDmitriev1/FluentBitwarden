namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal enum SshAgentMessageType : byte
{
    Failure = 5,
    Success = 6,

    RequestIdentities = 11,
    IdentitiesAnswer = 12,

    SignRequest = 13,
    SignResponse = 14,

    Extension = 27,
    ExtensionFailure = 28,
    ExtensionResponse = 29
}
