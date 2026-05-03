namespace FluentBitwarden.Modules.SshAgent.Models;

internal enum SshAgentMessageRequests : byte
{
    Unsupported = 0,

    RequestIdentities = 11,
    SignRequest = 13,

    AgenticExtension = 27
}

internal enum SshAgentMessageReplies : byte
{
    Undefined = 0,

    // replies
    Failure = 5,
    IdentitiesAnswer = 12,
    SignResponse = 14,
    ExtensionResponse = 29,
}