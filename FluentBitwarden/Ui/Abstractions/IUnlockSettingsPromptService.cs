namespace FluentBitwarden.Ui.Abstractions;

public interface IUnlockSettingsPromptService
{
    ValueTask<bool> ShowUnlockSettingsPromptAsync(CancellationToken cancellationToken = default);
}
