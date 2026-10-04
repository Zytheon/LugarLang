#if DEV
using LugarLang.Internal.Services.Developer;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace LugarLang.Mobile;

public partial class AppShell : Shell
{
#if DEV
    private readonly DeveloperOverlayManager
        developerOverlayManager;
#endif

    public AppShell()
    {
        InitializeComponent();

#if DEV
        developerOverlayManager =
            Microsoft.Maui.Controls.Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<
                    DeveloperOverlayManager>();
#endif
    }

    protected override void OnNavigated(
        ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

#if DEV
        if (CurrentPage
            is ContentPage contentPage)
        {
            developerOverlayManager.AttachToPage(
                contentPage);
        }
#endif

        if (CurrentState.Location
            .OriginalString
            .Contains("DiscoverPage"))
        {
            if (CurrentPage
                is Pages.DiscoverPage discoverPage)
            {
                discoverPage.Refresh();
            }
        }
    }
}