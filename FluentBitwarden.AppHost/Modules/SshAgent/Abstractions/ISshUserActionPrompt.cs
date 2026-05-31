using FluentBitwarden.AppHost.Modules.SshAgent.Models;
using FluentBitwarden.Contracts.Infrastructure.Shared;

namespace FluentBitwarden.Modules.SshAgent.Abstractions;

public interface ISshUserActionPrompt
{
    Task<UserActionDialogOutcome> PromptAsync(SshUserActionRequestViewModel request);
}

class TmpISshUserActionPrompt : ISshUserActionPrompt
{
    public Task<UserActionDialogOutcome> PromptAsync(SshUserActionRequestViewModel request)
    {
        throw new NotImplementedException();
    }
}