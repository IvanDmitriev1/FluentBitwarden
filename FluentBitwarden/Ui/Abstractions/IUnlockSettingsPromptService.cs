namespace FluentBitwarden.Ui.Abstractions;

/// <summary>
/// Shows the unlock-settings recommendation prompt to the user.
/// </summary>
public interface IUnlockSettingsPromptService
{
    /// <summary>
    /// Shows the unlock-settings prompt and returns whether settings should be opened.
    /// </summary>
    ValueTask<bool> ShowUnlockSettingsPromptAsync(CancellationToken cancellationToken = default);
}
