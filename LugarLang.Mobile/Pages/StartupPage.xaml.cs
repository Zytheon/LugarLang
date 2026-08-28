using Microsoft.Extensions.DependencyInjection;
using LugarLang.Mobile.Services.Developer;

namespace LugarLang.Mobile.Pages;

public partial class StartupPage : ContentPage
{
    public StartupPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(1000);

        DeveloperOverlayManager
            developerOverlayManager =
                Microsoft.Maui.Controls.Application.Current!
                    .Handler!
                    .MauiContext!
                    .Services
                    .GetRequiredService<
                        DeveloperOverlayManager>();

        Microsoft.Maui.Controls.Application.Current!.MainPage =
            new AppShell(
                developerOverlayManager);
    }
}