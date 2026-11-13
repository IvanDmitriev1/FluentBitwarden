using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Screens.Setup;

[Trait("Category", "Unit")]
public sealed class SetupEmailViewModelTests
{
    private readonly ISetupNavigator _navigator = Substitute.For<ISetupNavigator>();

    [Theory]
    [InlineData("")]
    [InlineData("missing-domain")]
    public void InvalidEmailShowsValidationAndKeepsTheCurrentPage(string email)
    {
        var viewModel = CreateViewModel(email);

        viewModel.ContinueCommand.Execute(null);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.EmailError));
        _navigator.DidNotReceive().ShowPassword(Arg.Any<SetupPasswordParameters>());
    }

    [Fact]
    public void ValidEmailNavigatesWithTheTrimmedAddress()
    {
        var viewModel = CreateViewModel($"  {TestCredentials.Email}  ");

        viewModel.ContinueCommand.Execute(null);

        Assert.Null(viewModel.EmailError);
        _navigator.Received(1).ShowPassword(Arg.Is<SetupPasswordParameters>(
            parameters => parameters.Email == TestCredentials.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://vault.example")]
    [InlineData("https://vault.example/?tenant=other")]
    public void InvalidCustomServerUrlShowsValidationAndKeepsTheCurrentPage(string serverUrl)
    {
        var viewModel = CreateViewModel(TestCredentials.Email);
        viewModel.SelectedServer = SetupServerOption.Custom;
        viewModel.CustomServerUrl = serverUrl;

        viewModel.ContinueCommand.Execute(null);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.ServerError));
        _navigator.DidNotReceive().ShowPassword(Arg.Any<SetupPasswordParameters>());
    }

    [Fact]
    public void ValidCustomServerUrlNavigatesWithItsEnvironmentAndEmailState()
    {
        const string customServerUrl = "  https://vault.example/  ";
        var viewModel = CreateViewModel(TestCredentials.Email);
        viewModel.SelectedServer = SetupServerOption.Custom;
        viewModel.CustomServerUrl = customServerUrl;

        viewModel.ContinueCommand.Execute(null);

        Assert.Null(viewModel.ServerError);
        _navigator.Received(1).ShowPassword(Arg.Is<SetupPasswordParameters>(parameters =>
            parameters.Context.Environment.ApiBase == new Uri("https://vault.example/api/") &&
            parameters.Context.Environment.VaultBase == new Uri("https://vault.example/") &&
            parameters.EmailState.CustomServerUrl == customServerUrl &&
            parameters.EmailState.SelectedServer == SetupServerOption.Custom));
    }

    private SetupEmailPageViewModel CreateViewModel(string email)
    {
        using var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IAccountClient>())
            .AddSingleton(Substitute.For<IWindowManager>())
            .AddSingleton(_navigator)
            .AddSingleton(Substitute.For<IAppSession>())
            .AddTransient<SetupEmailPageViewModel>()
            .BuildServiceProvider();

        var viewModel = services.GetRequiredService<SetupEmailPageViewModel>();
        viewModel.Email = email;
        return viewModel;
    }
}
