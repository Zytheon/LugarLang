using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services;
using LugarLang.Mobile.Services.Mapping;
using LugarLang.Mobile.Services.Mapping.Factories;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;
using LugarLang.Mobile.UI.Routing;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;

namespace LugarLang.Mobile.Pages;

public partial class MapPage : ContentPage
{
    private readonly MapControl mapControl;
    private readonly RouteLayerFactory routeLayerFactory;
    private readonly TransitDataService transitDataService;
    private readonly RouteAccessibilityService routeAccessibilityService;
    private readonly TripRoutingService tripRoutingService;
    private readonly JourneyRoutingService journeyRoutingService;
    private readonly RoutingVisualizationService routingVisualizationService;
    private readonly RoutingCandidateView routingCandidateView;

    private MemoryLayer? inboundLayer;
    private MemoryLayer? outboundLayer;
    private MemoryLayer? stopLayer;

    private MemoryLayer? fromLayer;
    private MemoryLayer? toLayer;

    private MemoryLayer? fromWalkingLayer;
    private MemoryLayer? toWalkingLayer;
    private MemoryLayer? ridingLayer;

    private Picker routePicker = null!;
    private Picker walkingDistancePicker = null!;

    private double maximumWalkingDistanceMeters = 500;

    private GeoPoint? fromPoint;
    private GeoPoint? toPoint;

    private List<DirectionEvaluation> currentCandidates =
        new();

    private enum PinMode
    {
        None,
        SettingFrom,
        SettingTo
    }

    private PinMode pinMode = PinMode.None;

    public MapPage()
    {
        InitializeComponent();

        routeLayerFactory =
            new RouteLayerFactory();

        transitDataService =
            new TransitDataService();

        routeAccessibilityService =
            new RouteAccessibilityService();

        tripRoutingService =
            new TripRoutingService(
                routeAccessibilityService);

        RouteTransferService routeTransferService =
            new RouteTransferService(
                routeAccessibilityService);

        journeyRoutingService =
            new JourneyRoutingService(
                tripRoutingService,
                routeTransferService);

        routingVisualizationService =
            new RoutingVisualizationService();

        routingCandidateView =
            new RoutingCandidateView();

        routingCandidateView.CandidateSelected +=
            RoutingCandidateView_CandidateSelected;

        mapControl =
            new MapControl();

        mapControl.Map?.Layers.Add(
            OpenStreetMap.CreateTileLayer());

        routePicker =
            new Picker
            {
                Title = "Debug route",
                Margin = new Thickness(5),
                HorizontalOptions =
                    LayoutOptions.Fill
            };

        foreach (
            Route route
            in transitDataService.Network.Routes)
        {
            routePicker.Items.Add(
                $"{route.Id} — {route.Name}");
        }

        routePicker.SelectedIndexChanged +=
            RoutePicker_SelectedIndexChanged;

        walkingDistancePicker =
            new Picker
            {
                Title = "Walking distance",
                Margin = new Thickness(5),
                WidthRequest = 130
            };

        walkingDistancePicker.Items.Add("250 m");
        walkingDistancePicker.Items.Add("500 m");
        walkingDistancePicker.Items.Add("750 m");
        walkingDistancePicker.Items.Add("1 km");

        walkingDistancePicker.SelectedIndex = 1;

        walkingDistancePicker.SelectedIndexChanged +=
            WalkingDistancePicker_SelectedIndexChanged;

        Button fromButton =
            new()
            {
                Text = "Set From",
                Margin = new Thickness(5)
            };

        fromButton.Clicked +=
            (sender, args) =>
            {
                pinMode =
                    PinMode.SettingFrom;
            };

        Button toButton =
            new()
            {
                Text = "Set To",
                Margin = new Thickness(5)
            };

        toButton.Clicked +=
            (sender, args) =>
            {
                pinMode =
                    PinMode.SettingTo;
            };

        mapControl.Info +=
            MapControl_Info;

        Grid controls =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(
                        GridLength.Star),

                    new ColumnDefinition(
                        GridLength.Auto),

                    new ColumnDefinition(
                        GridLength.Auto),

                    new ColumnDefinition(
                        GridLength.Auto)
                }
            };

        Grid.SetColumn(
            routePicker,
            0);

        Grid.SetColumn(
            walkingDistancePicker,
            1);

        Grid.SetColumn(
            fromButton,
            2);

        Grid.SetColumn(
            toButton,
            3);

        controls.Children.Add(
            routePicker);

        controls.Children.Add(
            walkingDistancePicker);

        controls.Children.Add(
            fromButton);

        controls.Children.Add(
            toButton);

        Grid mainContent =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(
                        GridLength.Star),

                    new ColumnDefinition(
                        new GridLength(320))
                }
            };

        Grid.SetColumn(
            mapControl,
            0);

        Grid.SetColumn(
            routingCandidateView,
            1);

        mainContent.Children.Add(
            mapControl);

        mainContent.Children.Add(
            routingCandidateView);

        Grid layout =
            new()
            {
                RowDefinitions =
                {
                    new RowDefinition(
                        GridLength.Auto),

                    new RowDefinition(
                        GridLength.Star)
                }
            };

        Grid.SetRow(
            controls,
            0);

        Grid.SetRow(
            mainContent,
            1);

        layout.Children.Add(
            controls);

        layout.Children.Add(
            mainContent);

        Content =
            layout;

        if (
            transitDataService.Network.Routes.Count > 0)
        {
            routePicker.SelectedIndex = 0;
        }
    }

    private void WalkingDistancePicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        switch (
            walkingDistancePicker.SelectedIndex)
        {
            case 0:
                maximumWalkingDistanceMeters = 250;
                break;

            case 1:
                maximumWalkingDistanceMeters = 500;
                break;

            case 2:
                maximumWalkingDistanceMeters = 750;
                break;

            case 3:
                maximumWalkingDistanceMeters = 1000;
                break;
        }

        RecalculateAccessibility();
    }

    private void MapControl_Info(
        object? sender,
        MapInfoEventArgs e)
    {
        if (pinMode == PinMode.None)
        {
            return;
        }

        MPoint position =
            e.WorldPosition;

        if (pinMode == PinMode.SettingFrom)
        {
            SetFromPin(position);

            pinMode =
                PinMode.None;

            return;
        }

        if (pinMode == PinMode.SettingTo)
        {
            SetToPin(position);

            pinMode =
                PinMode.None;
        }
    }

    private void SetFromPin(
        MPoint position)
    {
        if (fromLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                fromLayer);
        }

        var point =
            new NetTopologySuite.Geometries.Point(
                position.X,
                position.Y);

        var feature =
            new GeometryFeature
            {
                Geometry = point
            };

        feature.Styles.Add(
            new SymbolStyle
            {
                SymbolScale = 0.8,

                Fill =
                    new Mapsui.Styles.Brush(
                        Mapsui.Styles.Color.Blue),

                Outline =
                    new Pen(
                        Mapsui.Styles.Color.White,
                        2)
            });

        fromLayer =
            new MemoryLayer
            {
                Name = "From Pin",

                Features =
                    new List<IFeature>
                    {
                        feature
                    }
            };

        mapControl.Map?.Layers.Add(
            fromLayer);

        var geographic =
            Mapsui.Projections.SphericalMercator
                .ToLonLat(
                    position.X,
                    position.Y);

        fromPoint =
            new GeoPoint
            {
                Longitude =
                    geographic.lon,

                Latitude =
                    geographic.lat
            };

        RecalculateAccessibility();
    }

    private void SetToPin(
        MPoint position)
    {
        if (toLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                toLayer);
        }

        var point =
            new NetTopologySuite.Geometries.Point(
                position.X,
                position.Y);

        var feature =
            new GeometryFeature
            {
                Geometry = point
            };

        feature.Styles.Add(
            new SymbolStyle
            {
                SymbolScale = 0.8,

                Fill =
                    new Mapsui.Styles.Brush(
                        Mapsui.Styles.Color.Red),

                Outline =
                    new Pen(
                        Mapsui.Styles.Color.White,
                        2)
            });

        toLayer =
            new MemoryLayer
            {
                Name = "To Pin",

                Features =
                    new List<IFeature>
                    {
                        feature
                    }
            };

        mapControl.Map?.Layers.Add(
            toLayer);

        var geographic =
            Mapsui.Projections.SphericalMercator
                .ToLonLat(
                    position.X,
                    position.Y);

        toPoint =
            new GeoPoint
            {
                Longitude =
                    geographic.lon,

                Latitude =
                    geographic.lat
            };

        RecalculateAccessibility();
    }

    private void RecalculateAccessibility()
    {
        RemoveTripLayers();

        if (fromPoint == null ||
            toPoint == null)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 1: Both points exist.");

        if (
            transitDataService.Network.Routes.Count == 0)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 2: Routes exist.");

        currentCandidates =
            tripRoutingService.EvaluateAllTrips(
                transitDataService.Network.Routes,
                fromPoint,
                toPoint,
                maximumWalkingDistanceMeters);

        System.Diagnostics.Debug.WriteLine(
            $"ROUTING TEST 3: Evaluated {currentCandidates.Count} candidates.");

        DirectionEvaluation? bestTrip =
            tripRoutingService.SelectBestTrip(
                transitDataService.Network.Routes,
                fromPoint,
                toPoint,
                maximumWalkingDistanceMeters);

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 4: Best trip selected.");

        RoutingDebugSnapshot snapshot =
            routingVisualizationService.CreateSnapshot(
                currentCandidates,
                bestTrip,
                maximumWalkingDistanceMeters);

        System.Diagnostics.Debug.WriteLine(
            $"ROUTING TEST 5: Snapshot created with {snapshot.Candidates.Count} candidates.");

        routingCandidateView.Display(
            snapshot);

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 7: routingCandidateView.Display completed.");

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 6: Reached end.");
    }

    private void RoutingCandidateView_CandidateSelected(
        object? sender,
        RoutingDebugInfo candidate)
    {
        System.Diagnostics.Debug.WriteLine(
            $"CANDIDATE SELECTED: {candidate.RouteId} — {candidate.RouteName}");

        DirectionEvaluation? evaluation =
            currentCandidates.FirstOrDefault(
                item =>
                    item.Route.Id ==
                    candidate.RouteId &&

                    item.DirectionName ==
                    candidate.DirectionName);

        if (evaluation == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "CANDIDATE SELECTED: No matching DirectionEvaluation found.");

            return;
        }

        System.Diagnostics.Debug.WriteLine(
            "CANDIDATE SELECTED: Matching evaluation found.");

        DisplayTripRoute(
            evaluation.Route);

        DrawTrip(
            evaluation);
    }

    private void DrawTrip(
        DirectionEvaluation evaluation)
    {
        System.Diagnostics.Debug.WriteLine(
            "DRAW TRIP: Starting.");

        RemoveTripLayers();

        DrawWalkingConnections(
            evaluation.From,
            evaluation.To,
            evaluation.NearestFrom,
            evaluation.NearestTo);

        DrawRidingSegment(
            evaluation.Direction.Path,
            evaluation.FromIndex,
            evaluation.ToIndex);

        System.Diagnostics.Debug.WriteLine(
            "DRAW TRIP: Completed.");
    }

    private void DrawWalkingConnections(
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
        IList<GeoPoint> path,
        int fromIndex,
        int toIndex)
    {
        System.Diagnostics.Debug.WriteLine(
            $"DRAW RIDING SEGMENT: fromIndex={fromIndex}, toIndex={toIndex}, pathCount={path.Count}");

        if (
            fromIndex < 0 ||
            toIndex >= path.Count ||
            toIndex <= fromIndex)
        {
            System.Diagnostics.Debug.WriteLine(
                "DRAW RIDING SEGMENT: Invalid indices.");

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
            System.Diagnostics.Debug.WriteLine(
                "DRAW RIDING SEGMENT: Fewer than 2 coordinates.");

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

        System.Diagnostics.Debug.WriteLine(
            "DRAW RIDING SEGMENT: Layer added.");
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

    private void DisplayTripRoute(
        Route route)
    {
        RemoveCurrentRouteLayers();

        if (route.Stops.Count > 0)
        {
            stopLayer =
                routeLayerFactory.CreateStopLayer(
                    route.Stops);

            mapControl.Map?.Layers.Add(
                stopLayer);
        }

        CenterMapOnRoute();
    }

    private void RemoveTripLayers()
    {
        RemoveLayer(
            ref fromWalkingLayer);

        RemoveLayer(
            ref toWalkingLayer);

        RemoveLayer(
            ref ridingLayer);
    }

    private void RemoveLayer(
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

    private void RoutePicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        int selectedIndex =
            routePicker.SelectedIndex;

        if (
            selectedIndex < 0 ||
            selectedIndex >=
            transitDataService.Network.Routes.Count)
        {
            return;
        }

        Route route =
            transitDataService.Network.Routes[
                selectedIndex];

        DisplayDebugRoute(
            route);

        RecalculateAccessibility();
    }

    private void DisplayDebugRoute(
        Route route)
    {
        RemoveCurrentRouteLayers();

        if (route.Stops.Count > 0)
        {
            stopLayer =
                routeLayerFactory.CreateStopLayer(
                    route.Stops);

            mapControl.Map?.Layers.Add(
                stopLayer);
        }

        CenterMapOnRoute();
    }

    private void RemoveCurrentRouteLayers()
    {
        if (inboundLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                inboundLayer);

            inboundLayer = null;
        }

        if (outboundLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                outboundLayer);

            outboundLayer = null;
        }

        if (stopLayer != null)
        {
            mapControl.Map?.Layers.Remove(
                stopLayer);

            stopLayer = null;
        }

        RemoveTripLayers();
    }

    private void CenterMapOnRoute()
    {
        MRect? combinedExtent = null;

        if (stopLayer?.Extent != null)
        {
            combinedExtent =
                stopLayer.Extent;
        }

        if (combinedExtent == null)
        {
            return;
        }

        double centerX =
            (combinedExtent.MinX +
             combinedExtent.MaxX) /
            2.0;

        double centerY =
            (combinedExtent.MinY +
             combinedExtent.MaxY) /
            2.0;

        MPoint center =
            new(
                centerX,
                centerY);

        double width =
            combinedExtent.MaxX -
            combinedExtent.MinX;

        double height =
            combinedExtent.MaxY -
            combinedExtent.MinY;

        double resolution =
            Math.Max(
                width,
                height) /
            800.0;

        mapControl.Map?.Navigator
            .CenterOnAndZoomTo(
                center,
                resolution);
    }
}
