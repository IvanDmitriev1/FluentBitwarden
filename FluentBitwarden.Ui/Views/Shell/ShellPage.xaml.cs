using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Controls.VaultCiphers;
using FluentBitwarden.Views.Settings;
using FluentBitwarden.Views.Vault;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPage : Page, ILifeCycleAwarePage
{
    static ShellPage()
    {
        var pageByTag = new Dictionary<string, Type>
        {
            ["vault"] = typeof(VaultPage),
        };

        PageByTag = pageByTag;
        TagByPage = pageByTag.ToDictionary(static pair => pair.Value, static pair => pair.Key);
    }

    public ShellPage()
    {
        InitializeComponent();
    }

    private static readonly IReadOnlyDictionary<string, Type> PageByTag;
    private static readonly IReadOnlyDictionary<Type, string> TagByPage;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var param = e.Parameter as IPageNavigationParameter;
        Reload(param);
    }

    public void Reload(IPageNavigationParameter? parameter)
    {
        NavigateSection(typeof(VaultPage), parameter);
    }

    [RelayCommand]
    private void PaneToggle()
    {
        Nav.IsPaneOpen = !Nav.IsPaneOpen;
    }

    private void Nav_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateSection(typeof(SettingsPage));
            return;
        }

        string? tag = args.InvokedItemContainer?.Tag as string;
        if (tag is null || !PageByTag.TryGetValue(tag, out var pageType))
        {
            Debug.Fail($"No shell page is registered for tag '{tag}'.");
            return;
        }

        NavigateSection(pageType);
    }

    private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(SettingsPage))
        {
            Nav.SelectedItem = Nav.SettingsItem;
            return;
        }

        if (!TagByPage.TryGetValue(e.SourcePageType, out string? tag))
        {
            Nav.SelectedItem = null;
            return;
        }

        Nav.SelectedItem = Nav.MenuItems
            .Cast<NavigationViewItem>()
            .FirstOrDefault(item => (string)item.Tag == tag);

    }

    [RelayCommand]
    private void OnVaultCipherSelected(VaultCipherSearchBox.Selection selection)
    {
        NavigateSection(typeof(VaultPage),
            PageNavigationParameter.From(new ShowVaultCipherIntent(selection.QueryText, selection.SelectedItem)));
    }


    private void NavigateSection(Type pageType, IPageNavigationParameter? parameter = null)
    {
        if (ContentFrame.CurrentSourcePageType == pageType)
        {
            if (parameter is not null && ContentFrame.Content is ILifeCycleAwarePage page)
                page.Reload(parameter);

            return;
        }

        bool navigated = ContentFrame.Navigate(pageType, parameter);
        Debug.Assert(navigated, $"Shell navigation to {pageType.Name} failed.");
    }
}
