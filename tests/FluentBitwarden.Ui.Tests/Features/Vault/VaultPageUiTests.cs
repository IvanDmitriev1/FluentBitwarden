using BitwardenApi.Primitives.Ids;
using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using Microsoft.Windows.SDK.BuildTools.WinApp.UIAutomation;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Vault;

[Collection(DesktopUiCollection.Name)]
[Trait("Category", "Desktop")]
public sealed class VaultPageUiTests(DesktopUiFixture desktop)
{
    private static readonly UiSelector SearchBox = UiSelectors.ById("TextBox", "VaultCipherSearchBox");

    [Fact]
    public async Task RestoringSavedSelectionKeepsFocusInSearchBox()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var cipher = CreateCipher("startup-cipher", "Saved result");
        ConfigureSearch([cipher], [cipher]);
        desktop.SetVaultBrowseState(new VaultBrowseState("saved", cipher.Id));

        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await WaitForKeyboardFocusAsync(automation, SearchBox, cancellationToken);
        var row = await automation.WaitAsync(CipherRow(cipher.Name), "saved cipher row is selected", cancellationToken: cancellationToken);

        Assert.False(Assert.IsType<bool>(await automation.PropertyAsync(row, "HasKeyboardFocus", cancellationToken)));
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(row, "SelectionItemIsSelected", cancellationToken)));
        Assert.Equal("VaultCipherSearchBox", searchBox.AutomationId);
    }

    [Fact]
    public async Task KeyboardSubmissionFocusesTheSelectedRowAndArrowKeysMoveSelection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var requested = CreateCipher("keyboard-target", "Keyboard target");
        var next = CreateCipher("keyboard-next", "Keyboard next");
        ConfigureSearch([requested], [requested, next]);

        var automation = await StartUnlockedAsync(cancellationToken);
        var searchBox = await automation.WaitAsync(SearchBox, "search box is enabled and visible", element => element.IsEnabled && !element.IsOffscreen, cancellationToken);
        await automation.FocusAsync(searchBox, cancellationToken);
        await automation.SetValueAsync(searchBox, "keyboard", cancellationToken);
        await automation.WaitAsync(UiSelectors.ById("VaultCipherSuggestionName", "VaultCipherSearchBox"), "keyboard suggestion is visible", cancellationToken: cancellationToken);
        await desktop.SendKeysAsync("down enter", cancellationToken);

        var requestedRow = await WaitForKeyboardFocusAsync(automation, CipherRow(requested.Name), cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(requestedRow, "SelectionItemIsSelected", cancellationToken)));

        await desktop.SendKeysAsync("down", cancellationToken);
        var nextRow = await WaitForSelectedRowAsync(automation, next.Name, cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(nextRow, "HasKeyboardFocus", cancellationToken)));
    }

    [Fact]
    public async Task MouseSubmissionFocusesTheRequestedRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var requested = CreateCipher("mouse-target", "Mouse target");
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

        var row = await WaitForKeyboardFocusAsync(automation, CipherRow(requested.Name), cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(row, "SelectionItemIsSelected", cancellationToken)));
    }

    [Fact]
    public async Task OffscreenResultsAndRepeatedSubmissionsKeepFocusOnTheRequestedRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var results = Enumerable.Range(0, 30).Select(index => CreateCipher($"offscreen-{index}", $"Offscreen result {index}")).ToArray();
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
            var row = await WaitForKeyboardFocusAsync(automation, CipherRow(requested.Name), cancellationToken);
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
        Assert.Equal("VaultCipherSearchBox", searchBox.AutomationId);
    }

    private async Task<DesktopAutomation> StartUnlockedAsync(CancellationToken cancellationToken)
    {
        var account = UiTestComposition.CreateAccount();
        await desktop.PublishSessionStateAsync(new FluentBitwarden.Contracts.AppSession.State.AppSessionStateSnapshot.Unlocked(account), cancellationToken);
        var automation = await desktop.AutomationAsync(cancellationToken);
        await automation.WaitAsync(UiSelectors.ById("ShellNavigationView"), "vault shell is visible", element => !element.IsOffscreen, cancellationToken);
        return automation;
    }

    private void ConfigureSearch(Cipher[] suggestions, Cipher[] vaultResults) =>
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<VaultCipherQuery>().Limit == 8 ? suggestions : vaultResults));

    private static async Task<UiElement> WaitForKeyboardFocusAsync(
        DesktopAutomation automation,
        UiSelector selector,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            var element = await automation.WaitAsync(selector, "keyboard focus", cancellationToken: deadline.Token);
            if (await automation.PropertyAsync(element, "HasKeyboardFocus", deadline.Token) is true)
                return element;

            await Task.Delay(TimeSpan.FromMilliseconds(100), deadline.Token);
        }
    }

    private static async Task<UiElement> WaitForSelectedRowAsync(
        DesktopAutomation automation,
        string name,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        while (true)
        {
            var row = await automation.WaitAsync(CipherRow(name), "row is selected", cancellationToken: deadline.Token);
            if (await automation.PropertyAsync(row, "SelectionItemIsSelected", deadline.Token) is true)
                return row;

            await Task.Delay(TimeSpan.FromMilliseconds(100), deadline.Token);
        }
    }

    private static UiSelector CipherRow(string name) => new()
    {
        Query = name,
        ControlType = "ListItem",
        Root = UiSelectors.ById("VaultCipherListView"),
    };

    private static Cipher CreateCipher(string id, string name)
    {
        var cipher = Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = new CipherId(id);
        cipher.Name = name;
        return cipher;
    }
}
