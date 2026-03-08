using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Http;
using BitwaredApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BitwaredApi;

public static class BitwaredApiServiceCollectionExtensions
{
    public static IServiceCollection AddBitwaredApi(
        this IServiceCollection services,
        Action<BitwaredApiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        BitwaredApiOptions options = new();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IEnvironmentConfig>(_ => new EnvironmentConfig(options.Environment));
        services.AddSingleton<IClock>(_ => options.ClockOverride ?? new SystemClock());
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddSingleton<SessionCoordinator>();

        services.AddBitwaredHttpClients(options);

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IVaultService, VaultService>();
        services.AddSingleton<IBitwaredClient, BitwaredClient>();

        return services;
    }
}
