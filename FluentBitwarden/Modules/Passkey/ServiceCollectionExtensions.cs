using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.Passkey.Internal;
using FluentBitwarden.Modules.Passkey.Models;
using FluentBitwarden.Modules.Passkey.Services;
using FluentBitwarden.Shared.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Modules.Passkey;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPasskeyModule(this IServiceCollection services)
    {
        services.AddTransient<IPasskeyOverlayService, PasskeyOverlayService>();

        services.AddPipeMessageHandler<PasskeyAssertionHandler, PasskeyGetAssertionRequest, PasskeyAssertionResponse>(
            PasskeyJsonContext.Default.PasskeyGetAssertionRequest,
            PasskeyJsonContext.Default.PasskeyAssertionResponse);

        return services;
    }
}
