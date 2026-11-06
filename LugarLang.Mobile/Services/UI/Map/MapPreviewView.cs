
using Mapsui;
using Mapsui.Tiling;

namespace LugarLang.Mobile.UI.Map;

public sealed class MapPreviewView : ContentView
{
    private readonly Mapsui.UI.Maui.MapControl mapControl;

    public MapPreviewView()
    {
        mapControl = new Mapsui.UI.Maui.MapControl();

        mapControl.Map?.Layers.Add(
            OpenStreetMap.CreateTileLayer(
                "LugarLang/1.0 (fernandicokenrich@gmail.com)"));

        var center =
            Mapsui.Projections.SphericalMercator.FromLonLat(
                124.6319,
                8.4542);

        mapControl.Map?.Navigator.CenterOnAndZoomTo(
            new Mapsui.MPoint(center.x, center.y),
            100);

        Grid layout = new Grid();
        layout.Children.Add(mapControl);

        Label attribution = new Label
        {
            Text = "© OpenStreetMap contributors",
            FontSize = 10,
            TextColor = Colors.DarkSlateGray,
            BackgroundColor = Color.FromArgb("#EFFFFFFF"),
            Padding = new Thickness(5, 2),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(4)
        };

        layout.Children.Add(attribution);
        Content = layout;
    }
}
