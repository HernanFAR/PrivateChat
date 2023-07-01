using Fluxor;
using Microsoft.Extensions.Logging;
using Radzen;

namespace PrivateChat.MaUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddScoped<ContextMenuService>();
        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<NotificationService>();

        builder.Services.AddFluxor(options => options.ScanAssemblies(typeof(Core.Anchor).Assembly));

        return builder.Build();
    }
}
