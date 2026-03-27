using FluentBitwarden.Shared.Behaviors;
using FluentBitwarden.Shell;
using FluentBitwarden.Shell.Navigation;

namespace FluentBitwarden.Views.Loading;

public partial class LoadingPageViewModel(INavigationService navigationService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }


    public  async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        navigationService.NavigateTo<ShellPage>();
    }

    public Task OnUnloadingAsync()
    {
        return Task.CompletedTask;
    }
}