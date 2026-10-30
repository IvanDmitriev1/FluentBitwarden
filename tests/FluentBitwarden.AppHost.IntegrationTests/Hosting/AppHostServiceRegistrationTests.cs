using BitwardenApi;
using FluentBitwarden.AppHost.Application;
using FluentBitwarden.AppHost.Application.Abstractions;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.AppSession.Contracts;
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

        builder.Services.AddApplicationServices();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<ISettingsStore>(_ => SettingsStore.Instance);

        builder.Services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory("unused.db"));
        builder.Services.AddSingleton<IDatabaseInitializationService, SqliteInitializationService>();
        builder.Services.AddScoped<UnitOfWork>();
        builder.Services.AddScoped<IUnitOfWork>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddScoped<IDbSession>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddHttpClient("SharedHttpClient");
        builder.Services.AddSingleton<UiProcessLauncher>();
        builder.Services.AddSingleton<IUiProcessLauncher>(provider => provider.GetRequiredService<UiProcessLauncher>());
        builder.Services.AddSingleton<IAppHostTermination>(provider => provider.GetRequiredService<UiProcessLauncher>());
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
        builder.Services.AddHostedService<AppHostLifecycleService>();

        Assert.Single(builder.Services, descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(AppHostLifecycleService));

        using IHost host = builder.Build();

        Assert.NotNull(host.Services);
        using IServiceScope firstScope = host.Services.CreateScope();
        using IServiceScope secondScope = host.Services.CreateScope();
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<IAppSessionLeaseProvider>(),
            secondScope.ServiceProvider.GetRequiredService<IAppSessionLeaseProvider>());
    }
}
