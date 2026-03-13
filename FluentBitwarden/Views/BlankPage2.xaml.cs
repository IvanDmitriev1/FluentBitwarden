using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace FluentBitwarden.Views;

public sealed partial class BlankPage2 : Page
{
    public BlankPage2(BlankPage2ViewModel vm)
    {
        ViewModel = vm;
        DataContext = vm;
        InitializeComponent();
    }

    public BlankPage2ViewModel ViewModel { get; }
}

public partial class BlankPage2ViewModel(INavigationService navigationService) : ObservableObject, IPageLifecycleAware
{
    [RelayCommand]
    private void NavigateToPag1()
    {
        navigationService.Navigate<BlankPage1>();
    }

    [RelayCommand]
    private void NavigateToPag1Clean()
    {
        navigationService.Navigate<BlankPage1>(clearBackStack: true);
    }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine("Page2 loaded");
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        Debug.WriteLine("Page2 unloaded");
        return Task.CompletedTask;
    }
}