using FluentBitwarden.Contracts.Shared;
using FluentBitwarden.Resources.Dialogs.Models;

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