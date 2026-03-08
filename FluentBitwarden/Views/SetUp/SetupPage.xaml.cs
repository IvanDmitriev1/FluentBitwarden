using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels.SetUp;

namespace FluentBitwarden.Views.SetUp;

public sealed partial class SetupPage : CorePage
{
    public SetupPage(SetupPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SetupPageViewModel ViewModel { get; }
}
