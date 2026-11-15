using BitwardenApi.Client;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

internal static class TestAccounts
{
    public static AccountProfile CreateEuropean() => new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);

    public static AccountProfile CreateDesktop(string? email = null) => new(
        UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        email ?? TestCredentials.Email,
        new BitwardenEnvironment(
            new Uri("https://api.ui-test.example.test"),
            new Uri("https://identity.ui-test.example.test"),
            new Uri("https://notifications.ui-test.example.test"),
            new Uri("https://vault.ui-test.example.test")));
}
