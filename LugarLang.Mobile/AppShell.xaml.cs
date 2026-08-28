using LugarLang.Mobile.Services.Developer;

namespace LugarLang.Mobile;

public partial class AppShell : Shell
{
    private readonly DeveloperOverlayManager
        developerOverlayManager;

    public AppShell(
        DeveloperOverlayManager
            developerOverlayManager)
    {
        InitializeComponent();

        this.developerOverlayManager =
            developerOverlayManager;
    }

    protected override void OnNavigated(
        ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

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