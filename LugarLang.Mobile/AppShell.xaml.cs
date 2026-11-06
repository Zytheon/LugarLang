
#if DEV
using LugarLang.Internal.Services.Developer;
using Microsoft.Extensions.DependencyInjection;
using LugarLang.Internal.Pages;
using LugarLang.Internal.Services.Content;
using LugarLang.Mobile.Pages;
using LugarLang.Mobile.Services;
#endif

namespace LugarLang.Mobile;

public partial class AppShell : Shell
{
#if DEV
    private readonly DeveloperOverlayManager
        developerOverlayManager;

    private readonly IServiceProvider services;
#endif

    public AppShell()
    {
        InitializeComponent();

#if DEV
        services =
            Microsoft.Maui.Controls.Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        developerOverlayManager =
            services.GetRequiredService<
                DeveloperOverlayManager>();

        ConnectManagementPages();
#endif
    }

#if DEV
    private void ConnectManagementPages()
    {
        // Categories Management
        developerOverlayManager.CategoriesManagementRequested =
            async () =>
            {
                var page = new CategoryManagementPage(
                    services.GetRequiredService<CategoryContentService>(),
                    services.GetRequiredService<PlaceContentService>());

                await Navigation.PushAsync(page);
            };

        // Places Management
        developerOverlayManager.PlacesManagementRequested =
            async () =>
            {
                var page = new PlacesManagementPage(
                    services.GetRequiredService<PlaceContentService>(),
                    services.GetRequiredService<CategoryContentService>());

                await Navigation.PushAsync(page);
            };

        // Ride Hailing Management
        developerOverlayManager.RideHailingManagementRequested =
            async () =>
            {
                var page = new RideHailingManagementPage(
                    services.GetRequiredService<
                        RideHailingProviderContentService>());

                await Navigation.PushAsync(page);
            };
    }
#endif

    protected override void OnNavigated(
        ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

#if DEV
        if (CurrentPage is ContentPage contentPage)
        {
            developerOverlayManager.AttachToPage(
                contentPage);
        }
#endif
    }
}
