using FluentBitwarden.Shared.Navigation.Lifecycle;

namespace FluentBitwarden.Views.Shell.Offline;

public sealed partial class OfflinePage : LifecyclePage
{
    public OfflinePage(OfflinePageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public OfflinePageViewModel ViewModel { get; }
}
