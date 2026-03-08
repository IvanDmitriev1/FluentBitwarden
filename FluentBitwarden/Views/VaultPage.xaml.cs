using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels;

namespace FluentBitwarden.Views;

public sealed partial class VaultPage : CorePage
{
    public VaultPage(VaultPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public VaultPageViewModel ViewModel { get; }
}
