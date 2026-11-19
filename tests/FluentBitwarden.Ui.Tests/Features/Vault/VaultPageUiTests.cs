using BitwardenApi.Primitives.Ids;
using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Features.Settings;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using Microsoft.Windows.SDK.BuildTools.WinApp.UIAutomation;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Vault;

[Collection(DesktopUiCollection.Name)]
[Trait("Category", "Desktop")]
public sealed class VaultPageUiTests(DesktopUiFixture desktop)
{
    private static readonly UiSelector SearchBox = UiSelectors.ById("TextBox", "VaultCipherSearchBox");
    private static readonly UiSelector ListSearchBox = UiSelectors.ById("ListQueryBar.Search");

    [Fact]
    public async Task RestoringSavedSelectionKeepsFocusInSearchBox()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var cipher = TestCiphers.CreateLogin("startup-cipher", "Saved result");
        ConfigureSearch([cipher], [cipher]);
        desktop.SetVaultBrowseState(new VaultBrowseState("saved", cipher.Id));

        var automation = await StartUnlockedAsync(cancellationToken);
        await WaitForKeyboardFocusAsync(automation, SearchBox, cancellationToken);
        var row = await WaitForCipherRowAsync(automation, cipher.Name, "IsSelected", cancellationToken);

        Assert.False(Assert.IsType<bool>(await automation.PropertyAsync(row, "HasKeyboardFocus", cancellationToken)));
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(row, "IsSelected", cancellationToken)));
    }

    [Fact]
    public async Task RowSelectionAndFilterSurviveReopeningTheVaultShell()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var cipher = TestCiphers.CreateLogin("persisted-selection", "Persisted selection");
        const string filter = "saved";
        var expected = new VaultBrowseState(filter, cipher.Id);
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Cipher[]>(
                call.Arg<VaultCipherQuery>() is { Limit: null, SearchText: filter } ? [cipher] : []));

        var automation = await StartUnlockedAsync(cancellationToken);
        await EnterListFilterAsync(automation, expected.SearchText, cancellationToken);
        await ClickCipherRowAsync(automation, cipher.Name, cancellationToken);
        Assert.Equal(expected, desktop.VaultBrowseState);

        await desktop.DetachChildAsync(cancellationToken);
        Assert.Equal(expected, desktop.VaultBrowseState);

        await desktop.NavigateAsync(new AppSessionStateSnapshot.Unlocked(TestAccounts.CreateDesktop()), cancellationToken);
        await WaitForCipherRowAsync(automation, cipher.Name, "IsSelected", cancellationToken);
        var restoredSearch = await automation.WaitAsync(ListSearchBox, "restored filter is visible",
            element => !element.IsOffscreen && element.Value == expected.SearchText, cancellationToken);
        Assert.Equal(expected.SearchText, restoredSearch.Value);
    }

    [Fact]
    public async Task EscapeClearsTheSelectedRowAndSavesTheCurrentFilter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        const string filter = "filtered";
        var cipher = TestCiphers.CreateLogin("escape-selection", "Escape selection");
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Cipher[]>(
                call.Arg<VaultCipherQuery>() is { Limit: null, SearchText: filter } ? [cipher] : []));

        var automation = await StartUnlockedAsync(cancellationToken);
        await EnterListFilterAsync(automation, filter, cancellationToken);
        await ClickCipherRowAsync(automation, cipher.Name, cancellationToken);
        await WaitForCipherRowAsync(automation, cipher.Name, "HasKeyboardFocus", cancellationToken);
        Assert.Equal(new VaultBrowseState(filter, cipher.Id), desktop.VaultBrowseState);

        await desktop.SendKeysAsync("esc", cancellationToken);

        await WaitForCipherRowAsync(automation, cipher.Name, "IsSelected", cancellationToken, expected: false);
        Assert.Equal(new VaultBrowseState(filter, CipherId.Empty), desktop.VaultBrowseState);
    }

    [Fact]
    public async Task SortingTheVaultListKeepsTheSelectedCipherWithoutWritingSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        const string filter = "sort query";
        var first = TestCiphers.CreateLogin("sort-alpha", "Alpha account");
        var second = TestCiphers.CreateLogin("sort-zulu", "Zulu account");
        var ascending = new[] { first, second };
        var descending = new[]
        {
            TestCiphers.CreateLogin(second.Id.ToString(), second.Name),
            TestCiphers.CreateLogin(first.Id.ToString(), first.Name)
        };
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Cipher[]>(call.Arg<VaultCipherQuery>() switch
            {
                { Limit: null, SearchText: filter, SortDirection: VaultCipherSortDirection.Ascending } => ascending,
                { Limit: null, SearchText: filter, SortDirection: VaultCipherSortDirection.Descending } => descending,
                _ => []
            }));

        var expected = new VaultBrowseState(filter, first.Id);
        desktop.SetVaultBrowseState(expected);
        var automation = await StartUnlockedAsync(cancellationToken);
        await WaitForCipherRowAsync(automation, first.Name, "IsSelected", cancellationToken);
        var originalSecond = await automation.WaitAsync(CipherTitle(second.Name), "second row is visible",
            element => !element.IsOffscreen, cancellationToken);
        Assert.Equal(expected, desktop.VaultBrowseState);
        desktop.SettingsStore.ClearReceivedCalls();

        await desktop.SortVaultAsync(VaultCipherSortDirection.Descending, cancellationToken);

        var newFirst = await automation.WaitAsync(CipherTitle(second.Name), "Zulu row has moved up",
            element => !element.IsOffscreen && element.Y < originalSecond.Y, cancellationToken);
        var selectedRow = await WaitForCipherRowAsync(automation, first.Name, "IsSelected", cancellationToken);
        Assert.True(newFirst.Y < selectedRow.Y);
        Assert.Equal(expected, desktop.VaultBrowseState);
        desktop.SettingsStore.DidNotReceive().SetComposite(UiSettingKeys.Vault.StateKey, Arg.Any<VaultBrowseState>());
        _ = desktop.SettingsStore.DidNotReceive().RemoveComposite(UiSettingKeys.Vault.StateKey);
    }

    [Fact]
    public async Task KeyboardSubmissionFocusesTheSelectedRowAndArrowKeysMoveSelection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var requested = TestCiphers.CreateLogin("keyboard-target", "Keyboard target");
        var next = TestCiphers.CreateLogin("keyboard-next", "Keyboard next");
        ConfigureSearch([requested], [requested, next]);

        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await automation.WaitAsync(SearchBox, "search box is enabled and visible", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
        await automation.FocusAsync(searchBox, cancellationToken);
        await desktop.SendKeysAsync("keyboard", cancellationToken);
        var suggestion = UiSelectors.ById("VaultCipherSuggestionName", "VaultCipherSearchBox");
        await automation.WaitAsync(suggestion, "keyboard suggestion is visible", element => !element.IsOffscreen, cancellationToken);
        await desktop.SendKeysAsync("down", cancellationToken);
        await WaitForRowPropertyAsync(automation, suggestion, requested.Name, "IsSelected", cancellationToken);
        await desktop.SendKeysAsync("enter", cancellationToken);

        var requestedRow = await WaitForCipherRowAsync(automation, requested.Name, "HasKeyboardFocus", cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(requestedRow, "IsSelected", cancellationToken)));

        await desktop.SendKeysAsync("down", cancellationToken);
        var nextRow = await WaitForCipherRowAsync(automation, next.Name, "IsSelected", cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(nextRow, "HasKeyboardFocus", cancellationToken)));
    }

    [Fact]
    public async Task MouseSubmissionFocusesTheRequestedRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var requested = TestCiphers.CreateLogin("mouse-target", "Mouse target");
        ConfigureSearch([requested], [requested]);

        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await automation.WaitAsync(SearchBox, "search box is enabled and visible", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
        await automation.FocusAsync(searchBox, cancellationToken);
        await automation.SetValueAsync(searchBox, "mouse", cancellationToken);
        var suggestion = await automation.WaitAsync(
            UiSelectors.ById("VaultCipherSuggestionName", "VaultCipherSearchBox"),
            "mouse suggestion is visible",
            element => !element.IsOffscreen,
            cancellationToken);
        await automation.ClickAsync(suggestion, cancellationToken);

        var row = await WaitForCipherRowAsync(automation, requested.Name, "HasKeyboardFocus", cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(row, "IsSelected", cancellationToken)));
    }

    [Fact]
    public async Task OffscreenResultsAndRepeatedSubmissionsKeepFocusOnTheRequestedRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var results = Enumerable.Range(0, 30).Select(index => TestCiphers.CreateLogin($"offscreen-{index}", $"Offscreen result {index}")).ToArray();
        var requested = results[^1];
        ConfigureSearch([requested], results);

        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await automation.WaitAsync(SearchBox, "search box is enabled and visible", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);

        for (var submission = 0; submission < 2; submission++)
        {
            await automation.FocusAsync(searchBox, cancellationToken);
            await automation.SetValueAsync(searchBox, "offscreen", cancellationToken);
            var suggestion = await automation.WaitAsync(
                UiSelectors.ById("VaultCipherSuggestionName", "VaultCipherSearchBox"),
                "offscreen suggestion is visible",
                element => !element.IsOffscreen,
                cancellationToken);
            await automation.ClickAsync(suggestion, cancellationToken);
            var row = await WaitForCipherRowAsync(automation, requested.Name, "HasKeyboardFocus", cancellationToken);
            Assert.False(row.IsOffscreen);
        }
    }

    [Fact]
    public async Task MissingTargetDoesNotStealFocusAndNavigationAwayClearsTheRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        ConfigureSearch([], []);
        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await WaitForKeyboardFocusAsync(automation, SearchBox, cancellationToken);

        await desktop.RunOnUiThreadAsync(() =>
        {
            App.Current.GetRequiredService<Navis.WinUI.Abstractions.INavigation>()
                .Child?.Navigate<VaultPage, VaultCipherNavigationIntent>(
                    new ShowVaultCipherIntent("missing", new CipherId("missing-cipher")));
        }, cancellationToken);

        await WaitForKeyboardFocusAsync(automation, SearchBox, cancellationToken);
        var settingsItem = await automation.WaitAsync(
            new UiSelector { Query = "Settings", ControlType = "ListItem", Root = UiSelectors.ById("ShellNavigationView") },
            "settings navigation item is visible",
            cancellationToken: cancellationToken);
        await automation.InvokeAsync(settingsItem, cancellationToken);
        var focusedSettings = await WaitForKeyboardFocusAsync(automation,
            new UiSelector { Query = "Settings", ControlType = "ListItem", Root = UiSelectors.ById("ShellNavigationView") },
            cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);

        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(focusedSettings, "HasKeyboardFocus", cancellationToken)));
        Assert.False(Assert.IsType<bool>(await automation.PropertyAsync(searchBox, "HasKeyboardFocus", cancellationToken)));
    }

    [Fact]
    public async Task ExplicitSelectionShowsDetailsAndFilteringAndLockClearThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var cipher = TestCiphers.CreateLogin("detail-target", "Details target");
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<Cipher[]>(call.Arg<VaultCipherQuery>().SearchText == "no match" ? [] : [cipher]));

        var automation = await StartUnlockedAsync(cancellationToken);
        await desktop.RunOnUiThreadAsync(() =>
        {
            var navigation = App.Current.GetRequiredService<Navis.WinUI.Abstractions.INavigation>().Child;
            Assert.NotNull(navigation);
            navigation.Navigate<SettingsPage>();
            Assert.Equal(typeof(SettingsPage), navigation.CurrentPageType);
        }, cancellationToken);

        await NavigateToCipherAsync(cipher.Id, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("VaultDetailsName"),
            "selected cipher name is visible", element => element.Name == cipher.Name && !element.IsOffscreen, cancellationToken);
        _ = await desktop.VaultCipherClient.DidNotReceive().GetCipherAsync(
            Arg.Any<GetVaultCipherRequest>(), Arg.Any<CancellationToken>());

        var search = await automation.WaitAsync(
            UiSelectors.ById("ListQueryBar.Search"),
            "vault list search is visible", element => !element.IsOffscreen, cancellationToken);
        await automation.SetValueAsync(search, "no match", cancellationToken);
        await automation.WaitAbsentAsync(UiSelectors.ById("VaultDetailsName"), cancellationToken);

        await NavigateToCipherAsync(cipher.Id, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("VaultDetailsName"),
            "selected cipher name is visible before locking", element => element.Name == cipher.Name && !element.IsOffscreen, cancellationToken);

        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(TestAccounts.CreateDesktop()), cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("PasswordBoxInput", "AccountUnlockPasswordBox"),
            "lock replaces the vault shell", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
    }

    private Task NavigateToCipherAsync(CipherId id, CancellationToken cancellationToken) =>
        desktop.RunOnUiThreadAsync(() =>
        {
            App.Current.GetRequiredService<Navis.WinUI.Abstractions.INavigation>()
                .Child?.Navigate<VaultPage, VaultCipherNavigationIntent>(new ShowVaultCipherIntent("details", id));
        }, cancellationToken);

    private async Task<DesktopAutomation> StartUnlockedAsync(CancellationToken cancellationToken)
    {
        var account = TestAccounts.CreateDesktop();
        await desktop.PublishSessionStateAsync(new FluentBitwarden.Contracts.AppSession.State.AppSessionStateSnapshot.Unlocked(account), cancellationToken);
        var automation = await desktop.AutomationAsync(cancellationToken);
        await automation.WaitAsync(UiSelectors.ById("ShellNavigationView"), "vault shell is visible", element => !element.IsOffscreen, cancellationToken);
        return automation;
    }

    private void ConfigureSearch(Cipher[] suggestions, Cipher[] vaultResults) =>
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<VaultCipherQuery>().Limit == 8 ? suggestions : vaultResults));

    private async Task EnterListFilterAsync(DesktopAutomation automation, string filter, CancellationToken cancellationToken)
    {
        var toggle = await automation.WaitAsync(UiSelectors.ById("ListQueryBar.SearchToggle"),
            "list search toggle is enabled and visible", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
        await automation.InvokeAsync(toggle, cancellationToken);
        var search = await automation.WaitAsync(ListSearchBox, "list search is enabled and visible",
            element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
        await automation.FocusAsync(search, cancellationToken);
        await desktop.SendKeysAsync(filter, cancellationToken);
    }

    private static async Task ClickCipherRowAsync(DesktopAutomation automation, string name, CancellationToken cancellationToken)
    {
        var title = await automation.WaitAsync(CipherTitle(name), "cipher title is visible",
            element => !element.IsOffscreen, cancellationToken);
        var row = await automation.FindListItemAncestorAsync(title, cancellationToken);
        Assert.False(Assert.IsType<bool>(await automation.PropertyAsync(row, "IsSelected", cancellationToken)));
        await automation.ClickAsync(title, cancellationToken);
        await WaitForCipherRowAsync(automation, name, "IsSelected", cancellationToken);
    }

    private static async Task<UiElement> WaitForKeyboardFocusAsync(
        DesktopAutomation automation,
        UiSelector selector,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(DesktopAutomation.Timeout);
        while (true)
        {
            var element = await automation.WaitAsync(selector, "keyboard focus", cancellationToken: deadline.Token);
            if (await automation.PropertyAsync(element, "HasKeyboardFocus", deadline.Token) is true)
                return element;

            await Task.Delay(TimeSpan.FromMilliseconds(100), deadline.Token);
        }
    }

    private static Task<UiElement> WaitForCipherRowAsync(
        DesktopAutomation automation,
        string name,
        string property,
        CancellationToken cancellationToken,
        bool expected = true) =>
        WaitForRowPropertyAsync(automation, CipherTitle(name), name, property, cancellationToken, expected);

    private static UiSelector CipherTitle(string name) => new()
    {
        Query = name,
        ControlType = "Text",
        Root = UiSelectors.ById("CipherList"),
    };

    private static async Task<UiElement> WaitForRowPropertyAsync(
        DesktopAutomation automation,
        UiSelector titleSelector,
        string name,
        string property,
        CancellationToken cancellationToken,
        bool expected = true)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(DesktopAutomation.Timeout);
        try
        {
            while (true)
            {
                var title = await automation.WaitAsync(titleSelector, "cipher title is present", cancellationToken: deadline.Token);
                var row = await automation.FindListItemAncestorAsync(title, deadline.Token);
                if (await automation.PropertyAsync(row, property, deadline.Token) is bool value && value == expected)
                    return row;

                await Task.Delay(TimeSpan.FromMilliseconds(100), deadline.Token);
            }
        }
        catch (OperationCanceledException exception) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out after {DesktopAutomation.Timeout.TotalSeconds} seconds waiting for {property} on cipher row '{name}' to be {expected}.", exception);
        }
    }

}
