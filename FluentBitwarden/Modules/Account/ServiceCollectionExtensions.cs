using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Account;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, AccountRepository>();


        return services;
    }
}