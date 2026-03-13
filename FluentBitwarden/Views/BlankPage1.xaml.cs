using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

public sealed partial class BlankPage1 : Page
{
    public BlankPage1(BlankPage1ViewModel vm)
    {
        ViewModel = vm;
        DataContext = vm;
        InitializeComponent();
    }

    public BlankPage1ViewModel ViewModel { get; }
}

public partial class BlankPage1ViewModel(INavigationService navigationService) : ObservableObject, IPageLifecycleAware
{
    [RelayCommand]
    private void NavigateToPage2()
    {
        navigationService.Navigate<BlankPage2>();
    }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        Debug.WriteLine("Page1 loaded");
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        Debug.WriteLine("Page1 unloaded");
        return Task.CompletedTask;
    }
}