namespace LugarLang.Mobile.Pages;

public class ChooseRegionPage : ContentPage
{
    private readonly DiscoverPage discoverPage;

    public ChooseRegionPage(
        DiscoverPage discoverPage)
    {
        this.discoverPage =
            discoverPage;

        Title = "Choose a region";

        VerticalStackLayout layout =
            new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 16
            };

        Label title =
            new Label
            {
                Text = "Choose a region",
                FontSize = 32,
                FontAttributes = FontAttributes.Bold
            };

        layout.Children.Add(title);

        AddRegionButton(
            layout,
            "Northern Mindanao");

        AddRegionButton(
            layout,
            "Central Visayas");

        AddRegionButton(
            layout,
            "Davao Region");

        AddRegionButton(
            layout,
            "Metro Manila");

        AddRegionButton(
            layout,
            "Western Visayas");

        AddRegionButton(
            layout,
            "More...");

        Content =
            new ScrollView
            {
                Content = layout
            };
    }

    private void AddRegionButton(
        VerticalStackLayout layout,
        string regionName)
    {
        Button button =
            new Button
            {
                Text = regionName,
                FontSize = 18,
                HeightRequest = 70
            };

        button.Clicked +=
            async (sender, e) =>
            {
                discoverPage.SetSelectedRegion(
                    regionName);

                await Navigation.PopAsync();
            };

        layout.Children.Add(button);
    }
}