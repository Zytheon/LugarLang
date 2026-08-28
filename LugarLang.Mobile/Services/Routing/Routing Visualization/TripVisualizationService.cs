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
    private readonly List<MemoryLayer> journeyLayers =
        new();

    public void DrawJourney(
        MapControl mapControl,
        Journey journey)
    {
        RemoveTripLayers(
            mapControl);

        if (journey.Legs.Count == 0)
        {
            return;
        }

        foreach (
            JourneyLeg leg
            in journey.Legs)
        {
            DrawJourneyLeg(
                mapControl,
                leg);
        }
    }

    public void RemoveTripLayers(
        MapControl mapControl)
    {
        foreach (
            MemoryLayer layer
            in journeyLayers)
        {
            mapControl.Map?.Layers.Remove(
                layer);
        }

        journeyLayers.Clear();
    }

    private void DrawJourneyLeg(
        MapControl mapControl,
        JourneyLeg leg)
    {
        DirectionEvaluation evaluation =
            leg.Evaluation;

        DrawWalkingConnection(
            mapControl,
            evaluation.From,
            evaluation.NearestFrom);

        DrawRidingSegment(
            mapControl,
            evaluation.Direction.Path,
            evaluation.FromIndex,
            evaluation.ToIndex);

        DrawWalkingConnection(
            mapControl,
            evaluation.NearestTo,
            evaluation.To);
    }

    private void DrawWalkingConnection(
        MapControl mapControl,
        GeoPoint start,
        GeoPoint end)
    {
        MPoint startPoint =
            Project(
                start);

        MPoint endPoint =
            Project(
                end);

        MemoryLayer layer =
            CreateWalkingLayer(
                "Walking Connection",
                startPoint,
                endPoint,
                Mapsui.Styles.Color.Green);

        journeyLayers.Add(
            layer);

        mapControl.Map?.Layers.Add(
            layer);
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

        List<Coordinate> coordinates =
            new();

        for (
            int i = fromIndex;
            i <= toIndex;
            i++)
        {
            MPoint projected =
                Project(
                    path[i]);

            coordinates.Add(
                new Coordinate(
                    projected.X,
                    projected.Y));
        }

        if (coordinates.Count < 2)
        {
            return;
        }

        LineString lineString =
            new(
                coordinates.ToArray());

        GeometryFeature feature =
            new()
            {
                Geometry =
                    lineString
            };

        feature.Styles.Add(
            new VectorStyle
            {
                Line =
                    new Pen(
                        Mapsui.Styles.Color.LightSkyBlue,
                        7)
            });

        MemoryLayer layer =
            new()
            {
                Name =
                    "Relevant Ride",

                Features =
                    new List<IFeature>
                    {
                        feature
                    }
            };

        journeyLayers.Add(
            layer);

        mapControl.Map?.Layers.Add(
            layer);
    }

    private MemoryLayer CreateWalkingLayer(
        string name,
        MPoint start,
        MPoint end,
        Mapsui.Styles.Color color)
    {
        List<IFeature> features =
            new();

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
                Name =
                    name,

                Features =
                    features
            };
        }

        double unitX =
            dx / length;

        double unitY =
            dy / length;

        const double dashLength =
            12.0;

        const double gapLength =
            8.0;

        double currentDistance =
            0;

        while (
            currentDistance < length)
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

            LineString lineString =
                new(
                    new[]
                    {
                        new Coordinate(
                            segmentStartPoint.X,
                            segmentStartPoint.Y),

                        new Coordinate(
                            segmentEndPoint.X,
                            segmentEndPoint.Y)
                    });

            GeometryFeature feature =
                new()
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
            Name =
                name,

            Features =
                features
        };
    }

    private MPoint Project(
        GeoPoint point)
    {
        var projected =
            Mapsui.Projections.SphericalMercator
                .FromLonLat(
                    point.Longitude,
                    point.Latitude);

        return new MPoint(
            projected.x,
            projected.y);
    }
}