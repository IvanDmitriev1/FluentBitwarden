using System.Security.Principal;
using FluentBitwarden.AppHost.Infrastructure.WindowsHelloIntegration.Internal;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class WindowsHelloKeyNameTests
{
    [Fact]
    public void Create_scopes_names_to_the_current_user_and_account()
    {
        using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
        string currentUserSid = currentIdentity.User?.Value
            ?? throw new InvalidOperationException("The test process has no Windows user SID.");
        const string firstAccountKeyName = "synthetic-account-one";
        const string secondAccountKeyName = "synthetic-account-two";

        Assert.Equal(
            $"{currentUserSid}//FluentBitwarden/AccountUnlock/{firstAccountKeyName}",
            WindowsHelloKeyName.Create(firstAccountKeyName));
        Assert.Equal(
            $"{currentUserSid}//FluentBitwarden/AccountUnlock/{secondAccountKeyName}",
            WindowsHelloKeyName.Create(secondAccountKeyName));
        Assert.NotEqual(
            WindowsHelloKeyName.Create(firstAccountKeyName),
            WindowsHelloKeyName.Create(secondAccountKeyName));
    }
}
