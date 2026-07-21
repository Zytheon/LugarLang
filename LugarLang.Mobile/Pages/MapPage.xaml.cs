using Mapsui.UI.Maui;
using Mapsui.Tiling;

namespace LugarLang.Mobile.Pages;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();

        var mapControl = new MapControl();

        mapControl.Map?.Layers.Add(
            OpenStreetMap.CreateTileLayer());

        Content = mapControl;
    }
}
