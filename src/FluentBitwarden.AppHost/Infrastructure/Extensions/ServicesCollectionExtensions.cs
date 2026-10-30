using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Infrastructure.Extensions;

internal static class ServicesCollectionExtensions
{
    public static void AddNotificationHandlers<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>(this IServiceCollection services)
        where T : class
    {
        services.AddScoped<T>();

        var handlerInterfaces = typeof(T)
            .GetInterfaces()
            .Where(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

        foreach (var handlerInterface in handlerInterfaces)
        {
            services.AddScoped(handlerInterface, static sp => sp.GetRequiredService<T>());
        }
    }
}
