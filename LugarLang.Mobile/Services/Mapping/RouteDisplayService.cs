using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping.Factories;
using Mapsui;
using Mapsui.Layers;
using Mapsui.UI.Maui;

namespace LugarLang.Mobile.Services.Mapping;

public class RouteDisplayService
{
    private readonly RouteLayerFactory routeLayerFactory;

    private MemoryLayer? stopLayer;

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

        if (route.Stops.Count > 0)
        {
            stopLayer =
                routeLayerFactory.CreateStopLayer(
                    route.Stops);

            mapControl.Map?.Layers.Add(
                stopLayer);
        }

        CenterMapOnPoints(
            mapControl,
            fromPoint,
            toPoint);
    }

    public void RemoveCurrentRouteLayers(
        MapControl mapControl)
    {
        if (stopLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                stopLayer);

            stopLayer = null;
        }
    }

    
}