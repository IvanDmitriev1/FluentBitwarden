namespace FluentBitwarden.Modules.SshAgent.Models;

internal enum SshAgentMessage : byte
{
    Undefined = 0,

    // replies
    Failure = 5,
    Success = 6,
    IdentitiesAnswer = 12,
    SignResponse = 14,
    ExtensionFailure = 28,
    ExtensionResponse = 29,


    // requests
    RequestIdentities = 11,
    SignRequest = 13,
    Lock = 22,
    Unlock = 23,
    Extension = 27
}