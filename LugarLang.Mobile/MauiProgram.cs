using CommunityToolkit.Maui;
using LugarLang.Internal.Services.Content;
using LugarLang.Internal.Services.Developer;
using LugarLang.Mobile.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using SkiaSharp.Views.Maui.Controls.Hosting;

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

        builder.Services.AddSingleton<CategoryContentService>();
        builder.Services.AddSingleton<PlaceContentService>();

        //API Services
        builder.Services.AddHttpClient<ContentSyncService>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7234");
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
