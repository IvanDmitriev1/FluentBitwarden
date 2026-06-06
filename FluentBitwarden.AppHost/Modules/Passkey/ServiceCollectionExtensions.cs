using FluentBitwarden.Contracts.Infrastructure.Ipc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Passkey;

internal static class ServiceCollectionExtensions
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "IPC registration intentionally reflects over known AppHost passkey handler methods.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "IPC registration intentionally closes known AppHost passkey handler invoker types at startup.")]
    public static IServiceCollection AddPasskeyModule(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<PasskeyClientHandler>();

        return services;
    }
}
