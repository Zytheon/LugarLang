using LugarLang.Mobile.UI.Settings;

namespace LugarLang.Mobile.Pages;

public class SettingsSubPage : ContentPage
{
    public SettingsSubPage(string title, View body, bool fill = false)
    {
        Title = title;
        BackgroundColor = UiColors.PageBackground;

        body.VerticalOptions = fill
            ? LayoutOptions.Fill
            : LayoutOptions.Start;

        Content = new Grid
        {
            Padding = new Thickness(18),
            Children = { body }
        };
    }
}