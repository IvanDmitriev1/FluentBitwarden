using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels.Setup;

namespace FluentBitwarden.Views.Setup;

public sealed partial class SetupPage : CorePage
{
    public SetupPage(SetupPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SetupPageViewModel ViewModel { get; }
}
