using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Views.Loading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services) =>
        services.AddView<LoadingPage, LoadingPageViewModel>();

    private static IServiceCollection AddView<TPage, TViewModel>(this IServiceCollection services)
        where TPage : Page
        where TViewModel : ObservableObject
    {
        services.AddTransient<TPage>();
        services.AddTransient<TViewModel>();

        return services;
    }
}