using FluentBitwarden.AppHost.Data.Abstractions;
using FluentBitwarden.AppHost.Data.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace FluentBitwarden.AppHost.Data;

internal static class ServiceCollectionExtensions
{
    private const string DatabaseFileName = "fluentbitwarden.db";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName)));
        services.AddSingleton<IDataInitializationService, DbUpDataInitializationService>();
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        return services;
    }
}
