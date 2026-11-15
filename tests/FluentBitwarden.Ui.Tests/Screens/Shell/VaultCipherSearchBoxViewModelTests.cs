using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Screens.Shell.Controls;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using Navis.WinUI.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Screens.Shell;

[Trait("Category", "Unit")]
public sealed class VaultCipherSearchBoxViewModelTests
{
    private readonly IVaultCipherClient _vaultClient = Substitute.For<IVaultCipherClient>();
    private readonly VaultCipherSearchBoxViewModel _viewModel;

    public VaultCipherSearchBoxViewModelTests()
    {
        _viewModel = new VaultCipherSearchBoxViewModel(
            _vaultClient,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<VaultCipherSearchBoxViewModel>>(),
            Substitute.For<INavigation>());
    }

    [Fact]
    public async Task SearchTrimsTextAndLimitsSuggestions()
    {
        var response = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var request = TestTaskSources.CreateAsyncContinuationSource<VaultCipherQuery>();
        _vaultClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                request.TrySetResult(call.Arg<VaultCipherQuery>());
                return response.Task;
            });

        _viewModel.SearchText = "  bank  ";

        VaultCipherQuery query = await request.Task;
        Assert.Equal("bank", query.SearchText);
        Assert.Equal(8, query.Limit);
        var expected = TestCiphers.CreateLogin(name: "Bank result");
        response.SetResult([expected]);
        await VaultCipherSearchBoxViewModelTestSupport.WaitUntilAsync(
            () => ReferenceEquals(_viewModel.Suggestions.SingleOrDefault(), expected));
    }

    [Fact]
    public void WhitespaceSearchClearsSuggestionsWithoutCallingVault()
    {
        _viewModel.Suggestions = [TestCiphers.CreateLogin(name: "old suggestion")];

        _viewModel.SearchText = "   ";

        Assert.Empty(_viewModel.Suggestions);
        _ = _vaultClient.DidNotReceive().SearchCiphersAsync(
            Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OlderSearchResultCannotReplaceNewerSuggestions()
    {
        var older = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var newer = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var olderStarted = TestTaskSources.CreateAsyncContinuationSource<CancellationToken>();
        var newerStarted = TestTaskSources.CreateAsyncContinuationSource<CancellationToken>();
        _vaultClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string searchText = call.Arg<VaultCipherQuery>().SearchText;
                CancellationToken token = call.Arg<CancellationToken>();
                return searchText switch
                {
                    "A" => VaultCipherSearchBoxViewModelTestSupport.StartSearch(olderStarted, older, token),
                    "B" => VaultCipherSearchBoxViewModelTestSupport.StartSearch(newerStarted, newer, token),
                    _ => throw new InvalidOperationException($"Unexpected search text: {searchText}"),
                };
            });

        Task olderSearch = _viewModel.SearchCommand.ExecuteAsync("A");
        CancellationToken olderToken = await olderStarted.Task;
        Task newerSearch = _viewModel.SearchCommand.ExecuteAsync("B");
        _ = await newerStarted.Task;
        var expected = TestCiphers.CreateLogin(name: "B result");
        newer.SetResult([expected]);
        await VaultCipherSearchBoxViewModelTestSupport.WaitUntilAsync(
            () => ReferenceEquals(_viewModel.Suggestions.SingleOrDefault(), expected));

        older.SetResult([TestCiphers.CreateLogin(name: "A result")]);
        await Task.WhenAll(olderSearch, newerSearch);
        Assert.Same(expected, Assert.Single(_viewModel.Suggestions));
        Assert.True(olderToken.IsCancellationRequested);
    }

    [Fact]
    public async Task CancelClearsSearchAndIgnoresLateResponse()
    {
        var response = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var started = TestTaskSources.CreateAsyncContinuationSource<CancellationToken>();
        _vaultClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => VaultCipherSearchBoxViewModelTestSupport.StartSearch(started, response, call.Arg<CancellationToken>()));

        Task search = _viewModel.SearchCommand.ExecuteAsync("search");
        CancellationToken token = await started.Task;
        _viewModel.CancelPendingSearchCommand.Execute(null);
        response.SetResult([TestCiphers.CreateLogin(name: "late result")]);
        await search;

        Assert.True(token.IsCancellationRequested);
        Assert.Equal(string.Empty, _viewModel.SearchText);
        Assert.Empty(_viewModel.Suggestions);
    }

    [Fact]
    public async Task SearchFailureDoesNotPreventLaterSearch()
    {
        var failed = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var recovered = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var failedStarted = TestTaskSources.CreateAsyncContinuationSource<bool>();
        var recoveredStarted = TestTaskSources.CreateAsyncContinuationSource<bool>();
        _vaultClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<VaultCipherQuery>().SearchText switch
            {
                "first" => VaultCipherSearchBoxViewModelTestSupport.StartSearch(failedStarted, failed),
                "recovery" => VaultCipherSearchBoxViewModelTestSupport.StartSearch(recoveredStarted, recovered),
                _ => throw new InvalidOperationException("Unexpected search."),
            });

        Task failedSearch = _viewModel.SearchCommand.ExecuteAsync("first");
        await failedStarted.Task;
        failed.SetException(new InvalidOperationException("Synthetic search failure."));
        await failedSearch;
        Task recoverySearch = _viewModel.SearchCommand.ExecuteAsync("recovery");
        await recoveredStarted.Task;
        var expected = TestCiphers.CreateLogin(name: "Recovered result");
        recovered.SetResult([expected]);
        await VaultCipherSearchBoxViewModelTestSupport.WaitUntilAsync(
            () => ReferenceEquals(_viewModel.Suggestions.SingleOrDefault(), expected));
        await recoverySearch;

        Assert.Same(expected, Assert.Single(_viewModel.Suggestions));
    }

}
