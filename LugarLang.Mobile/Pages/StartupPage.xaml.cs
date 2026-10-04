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

        System.Diagnostics.Debug.WriteLine(
            "DISCOVER ONAPPEARING FIRED");

        await Task.Delay(1000);

        Microsoft.Maui.Controls.Application.Current!.MainPage =
            new AppShell();
    }
}