using BitwardenApi;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.Hosting;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Integrations.SshAgent;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Account;
using FluentBitwarden.AppHost.Modules.Vault;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.IntegrationTests.Hosting;

public sealed class AppHostServiceRegistrationTests
{
    [Fact]
    public void AppHost_service_graph_builds_with_scope_validation_enabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<AppActivationArguments>(_ => null!);
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.Services.AddAppHostHosting();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<ISettingsStore>(_ => SettingsStore.Instance);

        builder.Services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory("unused.db"));
        builder.Services.AddSingleton<IDatabaseInitializationService, SqliteInitializationService>();
        builder.Services.AddScoped<UnitOfWork>();
        builder.Services.AddScoped<IUnitOfWork>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddScoped<IDbSession>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddHttpClient("SharedHttpClient");
        builder.Services.AddSingleton<IUiProcessLauncher, UiProcessLauncher>();
        builder.Services.AddBitwardenApi();
        builder.Services.AddAppSessionModule();
        builder.Services.AddAccountModule();
        builder.Services.AddVaultModule();

        builder.Services.AddSshAgentIntegration();

        builder.Services.AddIpcServer(IpcConstants.AppHostPipeName, handlers =>
        {
            handlers.Add<AppSessionIpcHandler>();
            handlers.Add<AccountIpcHandler>();
            handlers.Add<AccountWindowsHelloIntegrationIpcHandler>();
            handlers.Add<VaultOperationsIpcHandler>();
            handlers.Add<VaultCipherIpcHandler>();
            handlers.Add<VaultFolderIpcHandler>();
        });
        builder.Services.AddIpcEventServer(IpcConstants.AppHostEventsPipeName);
        builder.Services.AddIpcClient(IpcConstants.UiPipeName);

        using IHost host = builder.Build();

        Assert.NotNull(host.Services);
    }
}
