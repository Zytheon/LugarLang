using LugarLang.Mobile.Pages;
using LugarLang.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace LugarLang.Mobile.UI.Settings;

public class SettingsMenuView : ContentView
{
    private const double PanelWidth = 290;

    private readonly BoxView scrim;
    private readonly Border panel;

    public bool IsOpen { get; private set; }

    public SettingsMenuView()
    {
        IsVisible = false;

        scrim = new BoxView
        {
            BackgroundColor = Colors.Black,
            Opacity = 0
        };

        TapGestureRecognizer scrimTap = new();
        scrimTap.Tapped += async (_, _) => await CloseAsync();
        scrim.GestureRecognizers.Add(scrimTap);

        VerticalStackLayout items = new()
        {
            Spacing = 4,
            Padding = new Thickness(14, 56, 14, 24)
        };

        items.Children.Add(new Label
        {
            Text = "LugarLang",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = UiColors.TextPrimary,
            Margin = new Thickness(8, 0, 0, 16)
        });

        items.Children.Add(CreateItem("🛡️", "Permissions", () =>
            Push(new SettingsSubPage("Permissions", new PermissionsView()))));

        items.Children.Add(CreateItem("🌐", "Language", () =>
            Push(new SettingsSubPage("Language", new LanguageView()))));

        items.Children.Add(CreateItem("ℹ️", "About the app", () =>
            Push(new AboutPage())));

        items.Children.Add(new BoxView
        {
            HeightRequest = 1,
            BackgroundColor = UiColors.SecondaryBlue,
            Margin = new Thickness(8, 10)
        });

        items.Children.Add(CreateItem("📄", "Terms of service", () =>
            Push(new SettingsSubPage(
                "Terms of Service", new TermsView(), fill: true))));

        items.Children.Add(CreateItem("📝", "Survey form", OpenSurveyAsync));

        panel = new Border
        {
            WidthRequest = PanelWidth,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = UiColors.SurfaceWhite,
            StrokeThickness = 0,
            TranslationX = -PanelWidth,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(0, 24, 0, 24)
            },
            Content = items
        };

        Content = new Grid { Children = { scrim, panel } };
    }

    public async Task OpenAsync()
    {
        if (IsOpen) return;
        IsOpen = true;

        IsVisible = true;
        scrim.Opacity = 0;
        panel.TranslationX = -PanelWidth;

        await Task.WhenAll(
            scrim.FadeToAsync(0.45, 200),
            panel.TranslateToAsync(0, 0, 220, Easing.CubicOut));
    }

    public async Task CloseAsync()
    {
        if (!IsOpen) return;
        IsOpen = false;

        await Task.WhenAll(
            scrim.FadeToAsync(0, 180),
            panel.TranslateToAsync(-PanelWidth, 0, 200, Easing.CubicIn));

        IsVisible = false;
    }

    private View CreateItem(string icon, string text, Func<Task> action)
    {
        Grid row = new()
        {
            Padding = new Thickness(10, 12),
            ColumnSpacing = 14,
            BackgroundColor = Colors.Transparent
        };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        row.Add(new Label
        {
            Text = icon,
            FontSize = 18,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        row.Add(new Label
        {
            Text = text,
            FontSize = 16,
            TextColor = UiColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        TapGestureRecognizer tap = new();
        tap.Tapped += async (_, _) =>
        {
            await CloseAsync();
            await action();
        };
        row.GestureRecognizers.Add(tap);

        return row;
    }

    private static Task Push(Page page) =>
        Shell.Current.Navigation.PushAsync(page);

    private static async Task OpenSurveyAsync()
    {
        if (string.IsNullOrWhiteSpace(AppSettings.SurveyUrl))
        {
            await Shell.Current.DisplayAlertAsync(
                "Survey form",
                "The survey isn't available yet.",
                "OK");
            return;
        }

        await Launcher.Default.OpenAsync(new Uri(AppSettings.SurveyUrl));
    }
}