using Radzen;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ViewDependencyInstallerExtensions
{
    public static IServiceCollection AddViewDependencies(this IServiceCollection services)
    {
        services.AddScoped<ContextMenuService>();
        services.AddScoped<DialogService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<NotificationService>();

        return services;
    }
}
