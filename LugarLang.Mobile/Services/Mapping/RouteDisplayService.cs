using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping.Factories;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Maui;

namespace LugarLang.Mobile.Services.Mapping;

public class RouteDisplayService
{
    private readonly RouteLayerFactory routeLayerFactory;

    private MemoryLayer? inboundLayer;
    private MemoryLayer? outboundLayer;

    public RouteDisplayService(
        RouteLayerFactory routeLayerFactory)
    {
        this.routeLayerFactory =
            routeLayerFactory;
    }

    private void CenterMapOnPoints(
    MapControl mapControl,
    GeoPoint? fromPoint,
    GeoPoint? toPoint)
    {
        if (
            fromPoint == null ||
            toPoint == null)
        {
            return;
        }

        var fromProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    fromPoint.Longitude,
                    fromPoint.Latitude);

        var toProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    toPoint.Longitude,
                    toPoint.Latitude);

        double minX =
            Math.Min(
                fromProjected.x,
                toProjected.x);

        double maxX =
            Math.Max(
                fromProjected.x,
                toProjected.x);

        double minY =
            Math.Min(
                fromProjected.y,
                toProjected.y);

        double maxY =
            Math.Max(
                fromProjected.y,
                toProjected.y);

        double centerX =
            (minX + maxX) / 2.0;

        double centerY =
            (minY + maxY) / 2.0;

        double width =
            maxX - minX;

        double height =
            maxY - minY;

        double extent =
            Math.Max(
                width,
                height);

        // Prevent excessive zoom when
        // From and To are very close together.
        extent =
            Math.Max(
                extent,
                500);

        double resolution =
            extent / 800.0;

        mapControl.Map?.Navigator
            .CenterOnAndZoomTo(
                new MPoint(
                    centerX,
                    centerY),
                resolution);
    }
    public void DisplayRoute(
    MapControl mapControl,
    Route route,
    GeoPoint? fromPoint,
    GeoPoint? toPoint)
    {
        RemoveCurrentRouteLayers(
            mapControl);

        if (route.Inbound?.Path.Count > 1)
        {
            inboundLayer =
                BuildPathLayer(
                    route.Inbound.Path,
                    "Inbound Path",
                    Mapsui.Styles.Color.DodgerBlue);

            mapControl.Map?.Layers.Add(
                inboundLayer);
        }

        if (route.Outbound?.Path.Count > 1)
        {
            outboundLayer =
                BuildPathLayer(
                    route.Outbound.Path,
                    "Outbound Path",
                    Mapsui.Styles.Color.Orange);

            mapControl.Map?.Layers.Add(
                outboundLayer);
        }

        CenterMapOnPoints(
            mapControl,
            fromPoint,
            toPoint);
    }

    private MemoryLayer BuildPathLayer(
        IList<GeoPoint> path,
        string name,
        Mapsui.Styles.Color color)
    {
        List<NetTopologySuite.Geometries.Coordinate> coordinates =
            new();

        foreach (GeoPoint point in path)
        {
            var projected =
                Mapsui.Projections.SphericalMercator
                    .FromLonLat(
                        point.Longitude,
                        point.Latitude);

            coordinates.Add(
                new NetTopologySuite.Geometries.Coordinate(
                    projected.x,
                    projected.y));
        }

        NetTopologySuite.Geometries.LineString lineString =
            new(coordinates.ToArray());

        GeometryFeature feature =
            new()
            {
                Geometry = lineString
            };

        feature.Styles.Add(
            new VectorStyle
            {
                Line = new Pen(color, 5)
            });

        return new MemoryLayer
        {
            Name = name,
            Features = new List<IFeature> { feature }
        };
    }

    public void RemoveCurrentRouteLayers(
        MapControl mapControl)
    {
        if (inboundLayer != null)
        {
            mapControl.Map?.Layers.Remove(inboundLayer);
            inboundLayer = null;
        }

        if (outboundLayer != null)
        {
            mapControl.Map?.Layers.Remove(outboundLayer);
            outboundLayer = null;
        }
    }


}