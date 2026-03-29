using Windows.Storage;
using BitwardenApi.Modules.Identity.Models;
using Dapper;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Data.Migrations;
using FluentBitwarden.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Data;

internal static class ServiceCollectionExtensions
{
    private const string DatabaseFileName = "fluentbitwarden.db";

    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new UserId.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new EncryptedUserKey.DapperTypeHandler());
        SqlMapper.AddTypeHandler(new EncryptedPrivateKey.DapperTypeHandler());

        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(Path.Combine(ApplicationData.Current.LocalFolder.Path, DatabaseFileName)));
        services.AddSingleton<IDataInitializationService, DataInitializationService>();

        return services;
    }
}
