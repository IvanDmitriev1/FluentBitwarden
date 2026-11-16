using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault.Organizations;
using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Settings.Composition;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Ui.Features.Vault.Intents;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Features.Vault;

public sealed class VaultPageViewModelTests
{
    [Fact]
    public async Task NavigationWaitsForBrowseResults()
    {
        var token = TestContext.Current.CancellationToken;
        var client = Substitute.For<IVaultCipherClient>();
        var results = TestTaskSources.CreateAsyncContinuationSource<Cipher[]>();
        var cipher = TestCiphers.CreateLogin("requested");
        client.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>()).Returns(results.Task);
        var viewModel = new VaultPageViewModel(
            client, Substitute.For<ICompositeSettingsStore>(), NullLogger<VaultPageViewModel>.Instance,
            Substitute.For<IVaultFolderClient>(), Substitute.For<IVaultOrganizationClient>(), Substitute.For<IAccountClient>());

        var navigation = viewModel.OnNavigatedToAsync(new ShowVaultCipherIntent("requested", cipher.Id), token).AsTask();

        Assert.False(navigation.IsCompleted);
        results.SetResult([cipher]);
        await navigation.WaitAsync(token);
        Assert.Same(cipher, viewModel.SelectedCipher);
    }
}
