using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using Dapper;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Data.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace FluentBitwarden.Data;

internal static class ServiceCollectionExtensions
{
    private const string DatabaseFileName = "fluentbitwarden.db";

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new UserId.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new EncryptedUserKey.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new EncryptedPrivateKey.DapperTypeHandler());

        SqlMapper.AddTypeHandler(new FolderId.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new CollectionId.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new CipherId.DapperTypeHandler());

        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName)));
        services.AddSingleton<IDataInitializationService, DataInitializationService>();
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        return services;
    }
}
