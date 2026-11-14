using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Platform.Settings.Composition;
using FluentBitwarden.Ui.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Vault;

public sealed class VaultPageViewModelTests
{
    [Fact]
    public async Task CategorySelectionAppliesToTheCurrentSearch()
    {
        var client = Substitute.For<IVaultCipherClient>();
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<Cipher>()));
        var viewModel = new VaultPageViewModel(client, Substitute.For<ICompositeSettingsStore>(), NullLogger<VaultPageViewModel>.Instance)
        {
            SearchText = "example",
        };

        await viewModel.CategoryChangedCommand.ExecuteAsync(CipherType.Login);

        _ = client.Received(1).SearchCiphersAsync(
            Arg.Is<VaultCipherQuery>(query => query.SearchText == "example" && query.CipherType == CipherType.Login),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SortSelectionAppliesToTheCurrentSearchAndRetainsSelection()
    {
        var client = Substitute.For<IVaultCipherClient>();
        var cipher = Cipher.CreateBlankCipher(CipherType.Login);
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([cipher]));
        var viewModel = new VaultPageViewModel(client, Substitute.For<ICompositeSettingsStore>(), NullLogger<VaultPageViewModel>.Instance)
        {
            SearchText = "example",
        };

        viewModel.SelectedCipher = cipher;
        await viewModel.SortChangedCommand.ExecuteAsync(VaultCipherSortDirection.Descending);

        _ = client.Received(1).SearchCiphersAsync(
            Arg.Is<VaultCipherQuery>(query => query.SearchText == "example" && query.SortDirection == VaultCipherSortDirection.Descending),
            Arg.Any<CancellationToken>());
        Assert.Same(cipher, viewModel.SelectedCipher);
    }

    [Fact]
    public async Task SortingDoesNotReplaceASelectionMadeWhileTheSortIsLoading()
    {
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var first = CreateCipher("first");
        var second = CreateCipher("second");
        var sortedResults = new TaskCompletionSource<Cipher[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<VaultCipherQuery>().SortDirection == VaultCipherSortDirection.Descending
                ? sortedResults.Task
                : Task.FromResult<Cipher[]>([first, second]));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance);

        viewModel.SearchText = "example";
        viewModel.SelectedCipher = first;
        settings.ClearReceivedCalls();

        var sort = viewModel.SortChangedCommand.ExecuteAsync(VaultCipherSortDirection.Descending);
        viewModel.SelectedCipher = second;
        sortedResults.SetResult([CreateCipher("first"), CreateCipher("second")]);
        await sort.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(second.Id, viewModel.SelectedCipher?.Id);
        settings.Received(1).SetComposite(
            UiSettingKeys.Vault.StateKey, Arg.Is<VaultBrowseState>(state => state.SelectedCipherId == second.Id));
    }

    [Fact]
    public void DeselectionKeepsLastSavedSelection()
    {
        var settings = Substitute.For<ICompositeSettingsStore>();
        var viewModel = new VaultPageViewModel(
            Substitute.For<IVaultCipherClient>(), settings, NullLogger<VaultPageViewModel>.Instance)
        {
            SearchText = "example",
        };
        var cipher = CreateCipher("selected");

        viewModel.SelectedCipher = cipher;
        viewModel.SelectedCipher = null;

        Assert.Null(viewModel.SelectedCipher);
        settings.Received(1).SetComposite(
            UiSettingKeys.Vault.StateKey, Arg.Is<VaultBrowseState>(state => state.SearchText == "example" && state.SelectedCipherId == cipher.Id));
        _ = settings.DidNotReceive().RemoveComposite(UiSettingKeys.Vault.StateKey);
    }

    [Fact]
    public async Task ExplicitNavigationSelectionWritesBrowseStateOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var cipher = CreateCipher("requested");
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([cipher]));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance);

        await viewModel.OnNavigatedToAsync(new ShowVaultCipherIntent("requested query", cipher.Id), cancellationToken);

        settings.Received(1).SetComposite(
            UiSettingKeys.Vault.StateKey,
            Arg.Is<VaultBrowseState>(state => state.SearchText == "requested query" && state.SelectedCipherId == cipher.Id));
        _ = settings.DidNotReceive().RemoveComposite(UiSettingKeys.Vault.StateKey);
    }

    [Fact]
    public async Task SearchResetDoesNotEraseOrRewriteSavedBrowseState()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var savedCipher = CreateCipher("saved");
        settings.GetComposite(UiSettingKeys.Vault.StateKey)
            .Returns(new VaultBrowseState("saved query", savedCipher.Id));
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([savedCipher]));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance);
        await viewModel.OnNavigatedToAsync(cancellationToken);
        settings.ClearReceivedCalls();

        viewModel.SearchText = "new query";
        _ = client.Received(1).SearchCiphersAsync(
            Arg.Is<VaultCipherQuery>(query => query.SearchText == "new query"), Arg.Any<CancellationToken>());

        settings.DidNotReceive().SetComposite(
            Arg.Any<CompositeSettingKey<VaultBrowseState>>(), Arg.Any<VaultBrowseState>());
        _ = settings.DidNotReceive().RemoveComposite(Arg.Any<CompositeSettingKey<VaultBrowseState>>());
    }

    [Fact]
    public async Task NavigationSelectsTheRequestedResultAndIgnoresAnOlderSearch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var olderSearch = new TaskCompletionSource<Cipher[]>();
        var oldCipher = Cipher.CreateBlankCipher(CipherType.Login);
        var requestedCipher = Cipher.CreateBlankCipher(CipherType.Login);
        oldCipher.Id = new CipherId("old-cipher");
        requestedCipher.Id = new CipherId("requested-cipher");
        client.SearchCiphersAsync(Arg.Is<VaultCipherQuery>(query => query.SearchText == "old"), Arg.Any<CancellationToken>())
            .Returns(olderSearch.Task);
        client.SearchCiphersAsync(Arg.Is<VaultCipherQuery>(query => query.SearchText == "current"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([requestedCipher]));
        var viewModel = new VaultPageViewModel(client, Substitute.For<ICompositeSettingsStore>(), NullLogger<VaultPageViewModel>.Instance);

        viewModel.SearchText = "old";
        await viewModel.OnNavigatedToAsync(new ShowVaultCipherIntent("current", requestedCipher.Id), cancellationToken);
        olderSearch.SetResult([oldCipher]);
        _ = client.Received(1).SearchCiphersAsync(
            Arg.Is<VaultCipherQuery>(query => query.SearchText == "current"), Arg.Any<CancellationToken>());

        Assert.Equal("current", viewModel.SearchText);
        Assert.Same(requestedCipher, Assert.Single(viewModel.FilteredCiphers));
        Assert.Same(requestedCipher, viewModel.SelectedCipher);
    }

    [Fact]
    public async Task OrdinaryNavigationRestoresSavedSearchAndSelection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var cipher = Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = new CipherId("saved-cipher");
        settings.GetComposite(UiSettingKeys.Vault.StateKey)
            .Returns(new VaultBrowseState("saved query", cipher.Id));
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([cipher]));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance);

        await viewModel.OnNavigatedToAsync(cancellationToken);

        Assert.Equal("saved query", viewModel.SearchText);
        Assert.Same(cipher, viewModel.SelectedCipher);
        _ = client.Received(1).SearchCiphersAsync(
            Arg.Is<VaultCipherQuery>(query => query.SearchText == "saved query"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingRestoredSelectionRemovesStaleIdAndKeepsSearch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var missingId = new CipherId("missing-restored-cipher");
        settings.GetComposite(UiSettingKeys.Vault.StateKey)
            .Returns(new VaultBrowseState("saved query", missingId));
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<Cipher>()));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance);

        await viewModel.OnNavigatedToAsync(cancellationToken);

        settings.DidNotReceive().SetComposite(
            Arg.Any<CompositeSettingKey<VaultBrowseState>>(), Arg.Any<VaultBrowseState>());
        _ = settings.Received(1).RemoveComposite(Arg.Any<CompositeSettingKey<VaultBrowseState>>());
    }

    [Fact]
    public async Task MissingExplicitTargetPreservesSavedBrowseState()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var settings = Substitute.For<ICompositeSettingsStore>();
        var previous = Cipher.CreateBlankCipher(CipherType.Login);
        previous.Id = new CipherId("previous-cipher");
        var missing = new CipherId("missing-explicit-cipher");
        settings.GetComposite(UiSettingKeys.Vault.StateKey)
            .Returns(new VaultBrowseState("saved query", previous.Id));
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([previous]));
        var viewModel = new VaultPageViewModel(client, settings, NullLogger<VaultPageViewModel>.Instance)
        {
            SearchText = "saved query",
            SelectedCipher = previous,
        };
        settings.ClearReceivedCalls();

        await viewModel.OnNavigatedToAsync(new ShowVaultCipherIntent("missing query", missing), cancellationToken);

        Assert.Equal("missing query", viewModel.SearchText);
        Assert.Null(viewModel.SelectedCipher);
        settings.DidNotReceive().SetComposite(
            Arg.Any<CompositeSettingKey<VaultBrowseState>>(), Arg.Any<VaultBrowseState>());
        _ = settings.DidNotReceive().RemoveComposite(Arg.Any<CompositeSettingKey<VaultBrowseState>>());
    }

    private static Cipher CreateCipher(string id)
    {
        var cipher = Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = new CipherId(id);
        return cipher;
    }
}
