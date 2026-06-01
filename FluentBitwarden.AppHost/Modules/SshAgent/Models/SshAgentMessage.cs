namespace FluentBitwarden.AppHost.Modules.SshAgent.Models;

public enum SshAgentMessageRequests : byte
{
    Unsupported = 0,

    RequestIdentities = 11,
    SignRequest = 13,

    AgenticExtension = 27
}

public enum SshAgentMessageReplies : byte
{
    Undefined = 0,

    // replies
    Failure = 5,
    IdentitiesAnswer = 12,
    SignResponse = 14,
    ExtensionResponse = 29,
}