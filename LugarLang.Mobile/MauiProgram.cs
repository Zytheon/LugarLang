using LugarLang.Mobile.Services.Developer;
using Microsoft.Extensions.Logging;
using Shiny;
using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui;

namespace LugarLang.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
    .UseMauiCommunityToolkit(options =>
    {
        options.SetPopupDefaults(new DefaultPopupSettings
        {
            Margin = 0,
            Padding = 0
        });
    })
            .UseShinyControls()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services.AddSingleton<
    DeveloperOverlayManager>();
        builder.Services.AddSingleton<
    DeveloperLayoutPersistenceService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
