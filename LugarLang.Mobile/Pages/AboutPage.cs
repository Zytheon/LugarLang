using LugarLang.Mobile.UI.Settings;

namespace LugarLang.Mobile.Pages;

public class AboutPage : ContentPage
{
    public AboutPage()
    {
        Title = "About";
        BackgroundColor = UiColors.PageBackground;

        VerticalStackLayout body = new()
        {
            Padding = new Thickness(20),
            Spacing = 22
        };

        body.Children.Add(new Label
        {
            Text = "LugarLang",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = UiColors.TextPrimary
        });

        body.Children.Add(new Label
        {
            Text = $"Version {AppInfo.Current.VersionString}",
            FontSize = 13,
            TextColor = UiColors.TextSecondary
        });

        // PLACEHOLDER text: edit freely.
        body.Children.Add(Section("About",
            "LugarLang is a public transportation and discovery app " +
            "that serves as a soft infrastructure, allowing non-locals" +
            "to navigate and commute in major cities. It " +
            "serves as a cheaper alternative to point-to-point ride-hailing " +
            "apps, and incorporates discovery by pictures, " +
            "rather than relying solely on mapping, for people who " +
            "are unfamiliar with the local road layout, thus " +
            "providing an interface that caters to the users needs."));

        body.Children.Add(Section("Credits",
            "Map data © OpenStreetMap contributors.\n\n" +
            "Jeepney route data is based on the CDO Local Public " +
            "Transport Route Plan and has not been fully verified.\n\n" +
            "Developed by: Kenrich Fernandico\n\n" +
            "Built with .NET MAUI."));

        Content = new ScrollView { Content = body };
    }

    private static View Section(string heading, string text) =>
        new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = heading,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = UiColors.TextPrimary
                },
                new Label
                {
                    Text = text,
                    FontSize = 14,
                    LineHeight = 1.3,
                    TextColor = UiColors.TextSecondary
                }
            }
        };
}