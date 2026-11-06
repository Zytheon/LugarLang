using Microsoft.Maui.Controls.Shapes;

namespace LugarLang.Mobile.UI.Settings;

public class TermsView : ContentView
{
    // PLACEHOLDER: replace with your real terms.
    private const string TermsText =
        "Last updated: [November 11, 2026]\n\n" +
        "1. About LugarLang\n" +
        "LugarLang is a guide to places and local transport. " +
        "Information is provided for general reference only.\n\n" +
        "2. Accuracy\n" +
        "Place details and jeepney routes may be incomplete, outdated or " +
        "wrong. Routes and travel times are estimates, not guarantees. " +
        "Use your own judgement and check with drivers or locals.\n\n" +
        "3. Location\n" +
        "With your permission, the app uses your device location to show " +
        "places near you. You can turn this off in your device settings " +
        "at any time.\n\n" +
        "4. Your use\n" +
        "Do not misuse the app, interfere with its operation, or copy its " +
        "content for commercial use without permission.\n\n" +
        "5. Changes\n" +
        "These terms may be updated. Continuing to use the app means you " +
        "accept the updated terms.\n\n" +
        "6. Contact\n" +
        "fernandicokenrich@gmail.com";

    public TermsView()
    {
        Content = new Border
        {
            Padding = new Thickness(16),
            Stroke = UiColors.BorderBlue,
            StrokeThickness = 1,
            BackgroundColor = UiColors.SurfaceWhite,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new ScrollView
            {
                Content = new Label
                {
                    Text = TermsText,
                    FontSize = 13,
                    LineHeight = 1.3,
                    TextColor = UiColors.TextPrimary
                }
            }
        };
    }
}