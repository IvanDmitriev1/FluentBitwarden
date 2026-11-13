using BitwardenApi.Client;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Tests.TestInfrastructure;

namespace FluentBitwarden.Ui.Tests.Screens.Shell;

[Trait("Category", "Unit")]
public sealed class ShellViewModelTests
{
    [Fact]
    public async Task NavigationStoresAndClearsTheAccount()
    {
        var viewModel = new ShellPageViewModel();
        var account = new AccountProfile(
            UserId.Parse("11111111-1111-1111-1111-111111111111"),
            TestCredentials.Email,
            BitwardenEnvironment.Europe);

        await viewModel.OnNavigatedToAsync(account, CancellationToken.None);
        Assert.Same(account, viewModel.Account);

        await viewModel.OnNavigatedFromAsync(CancellationToken.None);
        Assert.Null(viewModel.Account);
    }
}
