using LugarLang.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace LugarLang.Mobile.UI.Settings;

public class LanguageView : ContentView
{
    private readonly VerticalStackLayout list = new() { Spacing = 10 };

    public LanguageView()
    {
        Content = list;
        Build();
    }

    private void Build()
    {
        list.Children.Clear();

        foreach ((string code, string name) in AppSettings.Languages)
        {
            bool selected = code == AppSettings.Language;

            Grid row = new();
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            row.Add(new Label
            {
                Text = name,
                FontSize = 16,
                TextColor = UiColors.TextPrimary
            }, 0, 0);

            row.Add(new Label
            {
                Text = selected ? "✓" : "",
                FontSize = 18,
                TextColor = UiColors.PrimaryBlue
            }, 1, 0);

            Border card = new()
            {
                Padding = new Thickness(16, 14),
                StrokeThickness = 1,
                Stroke = selected ? UiColors.PrimaryBlue : UiColors.BorderBlue,
                BackgroundColor = selected
                    ? UiColors.SecondaryBlue
                    : UiColors.SurfaceWhite,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Content = row
            };

            string chosen = code;
            TapGestureRecognizer tap = new();
            tap.Tapped += (_, _) =>
            {
                AppSettings.Language = chosen;
                Build();
            };
            card.GestureRecognizers.Add(tap);

            list.Children.Add(card);
        }
    }
}