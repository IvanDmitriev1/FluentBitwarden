using BitwardenApi;
using BitwardenApi.Notifications.Abstractions;
using FluentBitwarden.AppHost.Application;
using FluentBitwarden.AppHost.Application.Abstractions;
using FluentBitwarden.AppHost.AppSession;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Integrations.SshAgent;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Account;
using FluentBitwarden.AppHost.Modules.Vault;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Platform.Ipc;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.IntegrationTests.Hosting;

public sealed class AppHostServiceRegistrationTests
{
    [Fact]
    public async Task AppHost_service_graph_builds_with_scope_validation_enabled()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<AppActivationArguments>(_ => null!);
        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        builder.Services.AddApplicationServices();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(Substitute.For<ISettingsStore>());
        builder.Services.AddSingleton<IUserDialogService, UserDialogService>();

        builder.Services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory("unused.db"));
        builder.Services.AddSingleton<IDatabaseInitializationService, SqliteInitializationService>();
        builder.Services.AddScoped<UnitOfWork>();
        builder.Services.AddScoped<IUnitOfWork>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddScoped<IDbSession>(static provider => provider.GetRequiredService<UnitOfWork>());
        builder.Services.AddHttpClient("SharedHttpClient");
        builder.Services.AddSingleton<IUiProcessLauncher>(new FakeUiProcessLauncher());
        builder.Services.AddSingleton<IAppHostTermination>(Substitute.For<IAppHostTermination>());
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
        ServiceDescriptor dialogServiceRegistration = Assert.Single(builder.Services, descriptor =>
            descriptor.ServiceType == typeof(IUserDialogService) &&
            descriptor.ImplementationType == typeof(UserDialogService));
        Assert.Equal(ServiceLifetime.Singleton, dialogServiceRegistration.Lifetime);
        await using AsyncServiceScope firstScope = host.Services.CreateAsyncScope();
        await using AsyncServiceScope secondScope = host.Services.CreateAsyncScope();
        IAppSessionAccessor firstAccessor = firstScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>();
        Assert.Same(firstAccessor, firstScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>());
        Assert.NotSame(firstAccessor, secondScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>());
    }

    [Fact]
    public async Task Registered_scopes_hold_the_retired_state_until_the_last_scope_disposes()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IIpcEventPublisher>());
        services.AddSingleton(Substitute.For<IUiProcessLauncher>());
        services.AddAppSessionModule();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var store = provider.GetRequiredService<AppSessionStore>();
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "scope@example.test", "first");
        var vault = Substitute.For<IUnlockedVault>();
        IAccountKeySession key = Substitute.For<IAccountKeySession>();
        key.UserId.Returns(account.UserId);
        IServerNotificationsDispatcher dispatcher = Substitute.For<IServerNotificationsDispatcher>();
        var state = new AppSessionState.Unlocked(account, vault, key, dispatcher);

        await store.AuthenticateAsync(account, CancellationToken.None);
        await store.UnlockAsync(state);
        AsyncServiceScope firstScope = provider.CreateAsyncScope();
        AsyncServiceScope secondScope = provider.CreateAsyncScope();
        IAppSessionAccessor firstAccessor = firstScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>();
        IAppSessionAccessor secondAccessor = secondScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>();

        try
        {
            Assert.Same(firstAccessor, firstScope.ServiceProvider.GetRequiredService<IAppSessionAccessor>());
            Assert.NotSame(firstAccessor, secondAccessor);
            Assert.Same(state, firstAccessor.State);
            Assert.Same(state, secondAccessor.State);

            await store.LockAsync(CancellationToken.None);
            Assert.True(state.Revoked.IsCancellationRequested);
            vault.DidNotReceive().Dispose();
            key.DidNotReceive().Dispose();

            await firstScope.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => _ = firstAccessor.State);
            vault.DidNotReceive().Dispose();
            key.DidNotReceive().Dispose();

            await secondScope.DisposeAsync();
            vault.Received(1).Dispose();
            key.Received(1).Dispose();
            await dispatcher.Received(1).DisposeAsync();
        }
        finally
        {
            await secondScope.DisposeAsync();
            await firstScope.DisposeAsync();
            await store.SignOutAsync(CancellationToken.None);
        }
    }
}
