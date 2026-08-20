using CdoGtfsConverter.Models;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using NetTopologySuite.Geometries;

namespace LugarLang.Mobile.Services.Mapping;

public class MapPinService
{
    public (MemoryLayer Layer, GeoPoint Point) CreatePin(
        MPoint position,
        string layerName,
        Mapsui.Styles.Color color)
    {
        var mapPoint =
    new NetTopologySuite.Geometries.Point(
        position.X,
        position.Y);

        var feature =
            new GeometryFeature
            {
                Geometry = mapPoint
            };

        feature.Styles.Add(
            new SymbolStyle
            {
                SymbolScale = 0.8,

                Fill =
                    new Mapsui.Styles.Brush(
                        color),

                Outline =
                    new Pen(
                        Mapsui.Styles.Color.White,
                        2)
            });

        var layer =
            new MemoryLayer
            {
                Name = layerName,

                Features =
                    new List<IFeature>
                    {
                        feature
                    }
            };

        var geographic =
            Mapsui.Projections.SphericalMercator
                .ToLonLat(
                    position.X,
                    position.Y);

        var geographicPoint =
            new GeoPoint
            {
                Longitude =
                    geographic.lon,

                Latitude =
                    geographic.lat
            };

        return (
            layer,
            geographicPoint);
    }
}



