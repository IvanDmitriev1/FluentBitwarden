using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Clipboard;
using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.Ui.Behaviours;

public sealed partial class CopyTextCommandResources : ResourceDictionary
{
    public CopyTextCommandResources() => InitializeComponent();

    private void OnCopyTextCanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args) =>
        args.CanExecute = CopyTextCommand.CanExecute(args.Parameter);

    private void OnCopyTextExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args) =>
        CopyTextCommand.Execute(
            args.Parameter,
            App.Current.GetRequiredService<IClipboardManager>(),
            App.Current.GetRequiredService<ISettingsStore>());
}
