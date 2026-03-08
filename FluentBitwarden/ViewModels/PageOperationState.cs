using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentBitwarden.ViewModels;

internal partial class PageOperationState : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public void ClearStatus()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void Reset()
    {
        IsBusy = false;
        ClearStatus();
    }

    public void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }

    public void ShowError(Exception exception)
        => ShowError(AuthErrorMessageFormatter.Format(exception));

    public async Task RunBusyAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        IsBusy = true;

        try
        {
            await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
