using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Modules.SshAgent.Abstractions;

public interface ISshUserActionPrompt
{
    Task<UserActionDialogOutcome> PromptAsync(SshUserActionRequestViewModel request);
}
