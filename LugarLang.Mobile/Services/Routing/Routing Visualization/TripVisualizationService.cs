using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Routing;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;

namespace LugarLang.Mobile.Services.RoutingVisualization;

public class TripVisualizationService
{
    private MemoryLayer? fromWalkingLayer;
    private MemoryLayer? toWalkingLayer;
    private MemoryLayer? ridingLayer;

    public void DrawTrip(
        MapControl mapControl,
        DirectionEvaluation evaluation)
    {
        RemoveTripLayers(mapControl);

        DrawWalkingConnections(
            mapControl,
            evaluation.From,
            evaluation.To,
            evaluation.NearestFrom,
            evaluation.NearestTo);

        DrawRidingSegment(
            mapControl,
            evaluation.Direction.Path,
            evaluation.FromIndex,
            evaluation.ToIndex);
    }

    public void RemoveTripLayers(
        MapControl mapControl)
    {
        RemoveLayer(
            mapControl,
            ref fromWalkingLayer);

        RemoveLayer(
            mapControl,
            ref toWalkingLayer);

        RemoveLayer(
            mapControl,
            ref ridingLayer);
    }

    private void DrawWalkingConnections(
        MapControl mapControl,
        GeoPoint from,
        GeoPoint to,
        GeoPoint nearestFrom,
        GeoPoint nearestTo)
    {
        var fromProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    from.Longitude,
                    from.Latitude);

        var toProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    to.Longitude,
                    to.Latitude);

        var nearestFromProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    nearestFrom.Longitude,
                    nearestFrom.Latitude);

        var nearestToProjected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    nearestTo.Longitude,
                    nearestTo.Latitude);

        MPoint fromMapPoint =
            new(
                fromProjected.x,
                fromProjected.y);

        MPoint toMapPoint =
            new(
                toProjected.x,
                toProjected.y);

        MPoint nearestFromMapPoint =
            new(
                nearestFromProjected.x,
                nearestFromProjected.y);

        MPoint nearestToMapPoint =
            new(
                nearestToProjected.x,
                nearestToProjected.y);

        fromWalkingLayer =
            CreateWalkingLayer(
                "Walking From",
                fromMapPoint,
                nearestFromMapPoint,
                Mapsui.Styles.Color.Green);

        toWalkingLayer =
            CreateWalkingLayer(
                "Walking To",
                nearestToMapPoint,
                toMapPoint,
                Mapsui.Styles.Color.Orange);

        mapControl.Map?.Layers.Add(
            fromWalkingLayer);

        mapControl.Map?.Layers.Add(
            toWalkingLayer);
    }

    private void DrawRidingSegment(
        MapControl mapControl,
        IList<GeoPoint> path,
        int fromIndex,
        int toIndex)
    {
        if (
            fromIndex < 0 ||
            toIndex >= path.Count ||
            toIndex <= fromIndex)
        {
            return;
        }

        var coordinates =
            new List<Coordinate>();

        for (
            int i = fromIndex;
            i <= toIndex;
            i++)
        {
            var projected =
                Mapsui.Projections.SphericalMercator
                    .FromLonLat(
                        path[i].Longitude,
                        path[i].Latitude);

            coordinates.Add(
                new Coordinate(
                    projected.x,
                    projected.y));
        }

        if (coordinates.Count < 2)
        {
            return;
        }

        var lineString =
            new LineString(
                coordinates.ToArray());

        var feature =
            new GeometryFeature
            {
                Geometry = lineString
            };

        feature.Styles.Add(
            new VectorStyle
            {
                Line =
                    new Pen(
                        Mapsui.Styles.Color.LightSkyBlue,
                        7)
            });

        ridingLayer =
            new MemoryLayer
            {
                Name = "Relevant Ride",

                Features =
                    new List<IFeature>
                    {
                        feature
                    }
            };

        mapControl.Map?.Layers.Add(
            ridingLayer);
    }

    private MemoryLayer CreateWalkingLayer(
        string name,
        MPoint start,
        MPoint end,
        Mapsui.Styles.Color color)
    {
        var features =
            new List<IFeature>();

        double dx =
            end.X -
            start.X;

        double dy =
            end.Y -
            start.Y;

        double length =
            Math.Sqrt(
                dx * dx +
                dy * dy);

        if (length == 0)
        {
            return new MemoryLayer
            {
                Name = name,
                Features = features
            };
        }

        double unitX =
            dx / length;

        double unitY =
            dy / length;

        double dashLength = 12.0;
        double gapLength = 8.0;

        double currentDistance = 0;

        while (currentDistance < length)
        {
            double segmentStart =
                currentDistance;

            double segmentEnd =
                Math.Min(
                    currentDistance +
                    dashLength,
                    length);

            MPoint segmentStartPoint =
                new(
                    start.X +
                    unitX * segmentStart,
                    start.Y +
                    unitY * segmentStart);

            MPoint segmentEndPoint =
                new(
                    start.X +
                    unitX * segmentEnd,
                    start.Y +
                    unitY * segmentEnd);

            var coordinates =
                new[]
                {
                    new Coordinate(
                        segmentStartPoint.X,
                        segmentStartPoint.Y),

                    new Coordinate(
                        segmentEndPoint.X,
                        segmentEndPoint.Y)
                };

            var lineString =
                new LineString(
                    coordinates);

            var feature =
                new GeometryFeature
                {
                    Geometry =
                        lineString
                };

            feature.Styles.Add(
                new VectorStyle
                {
                    Line =
                        new Pen(
                            color,
                            4)
                });

            features.Add(
                feature);

            currentDistance +=
                dashLength +
                gapLength;
        }

        return new MemoryLayer
        {
            Name = name,
            Features = features
        };
    }

    private void RemoveLayer(
        MapControl mapControl,
        ref MemoryLayer? layer)
    {
        if (layer == null)
        {
            return;
        }

        mapControl.Map?.Layers.Remove(
            layer);

        layer = null;
    }
}



