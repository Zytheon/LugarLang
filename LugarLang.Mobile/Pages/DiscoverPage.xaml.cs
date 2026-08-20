namespace LugarLang.Mobile.Pages;

public partial class DiscoverPage : ContentPage
{
    public DiscoverPage()
    {
        InitializeComponent();
    }

    private async void OnNearMeClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Near Me",
            "Nearby discovery will be implemented here.",
            "OK");
    }

    private async void OnInterestClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await DisplayAlertAsync(
                button.Text,
                "Interest discovery will be implemented here.",
                "OK");
        }
    }

    private async void OnRegionClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await DisplayAlertAsync(
                button.Text,
                "Region exploration will be implemented here.",
                "OK");
        }
    }
}


