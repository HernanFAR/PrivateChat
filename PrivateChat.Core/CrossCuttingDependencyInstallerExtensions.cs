using Blazored.SessionStorage;
using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.AspNetCore.Components.Authorization;
using PrivateChat.Core.Abstractions;
using PrivateChat.CrossCutting.ChatWebApi;
using VSlices.Core.Sender.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class CrossCuttingDependencyInstallerExtensions
{
    public static IServiceCollection AddCrossCuttingDependencies(this IServiceCollection services)
    {
        services.AddSender<ReflectionSender>();

        services.AddScoped(sp => new HttpClient());
        services
            .AddFluxor(options =>
            {
                options.ScanAssemblies(typeof(PrivateChat.Core.Anchor).Assembly);

#if DEBUG
                options.UseReduxDevTools();
#endif
            });

        services.AddScoped<ChatWebApiConnection>();
        services.AddScoped<ChatWebApiConnection.ChatHub>();
        services.AddScoped<LoginStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<LoginStateProvider>());
        services.AddScoped<IApplicationLoginProvider>(sp => sp.GetRequiredService<LoginStateProvider>());
        services.AddScoped<ISessionStorage, BrowserSessionStorage>();
        services.AddBlazoredSessionStorage();
        services.AddAuthorizationCore();
        services.AddSweetAlert2();

        return services;
    }
}
