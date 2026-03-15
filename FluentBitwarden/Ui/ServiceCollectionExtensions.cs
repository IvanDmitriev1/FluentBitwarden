using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Ui;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddView<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView>()
            where TPage : class
            where TView : ObservableObject
        {
            services.AddTransient<TPage>();
            services.AddTransient<TView>();
        }

        public void AddUiServices()
        {
            services.AddSingleton<INotificationService>(new NotificationService(TimeSpan.FromSeconds(10)));
            services.AddSingleton<INavigationService, FrameNavigationService>();
        }
    }
}
