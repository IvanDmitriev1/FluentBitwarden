using FluentBitwarden.AppHost.Infrastructure.Data.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace FluentBitwarden.AppHost.Infrastructure.Data;

internal static class ServiceCollectionExtensions
{
    private const string DatabaseFileName = "fluentbitwarden.db";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName);

        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(dbFilePath));
        services.AddSingleton<IDatabaseInitializationService, DbUpDatabaseInitializationService>();
        
        return services;
    }
}
