using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Extensions;

internal static class ServiceCollectionExtensions
{
    public static void AddView<TPage, TView>(this IServiceCollection services)
        where TPage : Page
        where TView : ObservableObject
    {
        services.AddTransient<TPage>();
        services.AddTransient<TView>();
    }
}