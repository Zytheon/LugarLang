using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Pages;

public partial class SplashPage : ContentPage
{
    private bool hasNavigated;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (hasNavigated)
            return;

        hasNavigated = true;

        await Task.Delay(2200);

        if (Microsoft.Maui.Controls.Application.Current
            is LugarLang.Mobile.App app)
        {
            app.ShowMainPage();
        }
    }
}