using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Offline;

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
