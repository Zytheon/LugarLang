using CdoGtfsConverter.Models;
using LugarLang.Mobile.Application.Routing;
using LugarLang.Mobile.Services.Mapping;
using LugarLang.Mobile.Services.Mapping.Factories;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingSpatial;
using LugarLang.Mobile.Services.RoutingVisualization;
using LugarLang.Mobile.Services.Transit;
using LugarLang.Mobile.Services.UI.Layout;
using LugarLang.Internal;
using LugarLang.Internal.Services;
using LugarLang.Mobile.Services;
using LugarLang.Mobile.UI.Routing;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices;

namespace LugarLang.Mobile.UI.Map;

public partial class MapPanelView : ContentView
{
    private readonly RouteDisplayService routeDisplayService;
    private readonly MapControl mapControl;
    private readonly RoutingCoordinator routingCoordinator;
    private readonly RouteLayerFactory routeLayerFactory;
    private readonly MapPinService mapPinService;
    private readonly MapInteractionController mapInteractionController;

    private readonly TransitDataService transitDataService;
    private readonly RouteAccessibilityService routeAccessibilityService;
    private readonly TripRoutingService tripRoutingService;

    private readonly SegmentGraphJourneyRoutingService segmentGraphJourneyRoutingService;
    private readonly RoutingCandidateService routingCandidateService;
    private readonly RoutingVisualizationService routingVisualizationService;
    private readonly RoutingCandidateView routingCandidateView;
    private readonly MapPageLayoutBuilder layoutBuilder;
    private readonly TripVisualizationService tripVisualizationService;

    private Picker routePicker = null!;
    private Picker walkingDistancePicker = null!;

    private Button fromButton = null!;
    private Button toButton = null!;

    private MemoryLayer? inboundLayer;
    private MemoryLayer? outboundLayer;
    private double maximumWalkingDistanceMeters = 500;

    private GeoPoint? fromPoint;
    private GeoPoint? toPoint;

    private List<DirectionEvaluation> currentCandidates = new();

    private List<Journey> currentJourneys = new();

    public MapPanelView()
    {
        InitializeComponent();

        routeLayerFactory =
            new RouteLayerFactory();

        routeDisplayService =
            new RouteDisplayService(
                routeLayerFactory);

        mapPinService =
            new MapPinService();

        mapInteractionController =
            new MapInteractionController();

        transitDataService =
            new TransitDataService();

        RouteGeometryIndex routeGeometryIndex =
            new RouteGeometryIndex(
                transitDataService.Network.Routes);

        routeAccessibilityService =
            new RouteAccessibilityService(
                routeGeometryIndex);

        tripRoutingService =
            new TripRoutingService(
                routeAccessibilityService);

        System.Diagnostics.Stopwatch graphLoadTimer =
            System.Diagnostics.Stopwatch.StartNew();

        RouteGraphAsset routeGraphAsset =
            new RouteGraphLoader().Load();

        RouteGraph routeGraph =
            new RouteGraph(
                routeGraphAsset,
                transitDataService.Network.Routes);

        graphLoadTimer.Stop();

        System.Diagnostics.Debug.WriteLine(
            $"[ROUTE GRAPH] Load + build: " +
            $"{graphLoadTimer.ElapsedMilliseconds}ms, " +
            $"{routeGraph.NodeCount} nodes");

        // One-time diagnostic dump -- lists every route/direction that
        // actually made it into the precomputed graph. If a route you
        // expect to see near a pin never appears in the candidate logs
        // during a search, check here first: if it's missing from THIS
        // list too, the problem is in build_graph.py / the asset, not
        // in the search. Comment out once you've confirmed what you
        // need -- it's verbose (one line per route-direction).
        routeGraph.LogRouteSummary();

        segmentGraphJourneyRoutingService =
            new SegmentGraphJourneyRoutingService(
                routeGraph);

        routingCandidateService =
            new RoutingCandidateService();

        routingVisualizationService =
            new RoutingVisualizationService(
                routingCandidateService);

        tripVisualizationService =
            new TripVisualizationService();

        routingCoordinator =
            new RoutingCoordinator(
                tripRoutingService,
                routingVisualizationService);

        routingCandidateView =
            new RoutingCandidateView();

        layoutBuilder =
            new MapPageLayoutBuilder();

        routingCandidateView.CandidateSelected +=
            RoutingCandidateView_CandidateSelected;

        mapControl =
            new MapControl();

        mapControl.Map?.Layers.Add(
            OpenStreetMap.CreateTileLayer(
                "LugarLang/1.0 (fernandicokenrich@gmail.com)"));

        var initialCenter =
    Mapsui.Projections.SphericalMercator
        .FromLonLat(
            124.6319,
            8.4542);

        mapControl.Map?.Navigator.CenterOnAndZoomTo(
            new MPoint(
                initialCenter.x,
                initialCenter.y),
            100);

        routePicker =
            new Picker
            {
                Title = "Find a route",
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

        fromButton =
            new Button
            {
                Text = "Set From",
                Margin = new Thickness(5)
            };

        fromButton.Clicked +=
            (sender, args) =>
            {
                mapInteractionController
                    .StartSettingFrom();
            };

        toButton =
            new Button
            {
                Text = "Set To",
                Margin = new Thickness(5)
            };

        toButton.Clicked +=
            (sender, args) =>
            {
                mapInteractionController
                    .StartSettingTo();
            };

        mapControl.Info +=
            MapControl_Info;

        BuildResponsiveLayout();
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
        if (!mapInteractionController.TryConsumeMapTap(
            e.WorldPosition,
            out MapInteractionController.PinMode mode))
        {
            return;
        }

        if (
            mode ==
            MapInteractionController.PinMode.SettingFrom)
        {
            SetFromPin(
                e.WorldPosition);

            return;
        }

        if (
            mode ==
            MapInteractionController.PinMode.SettingTo)
        {
            SetToPin(
                e.WorldPosition);
        }
    }

    private void SetFromPin(
        MPoint position,
        bool recalculate = true)
    {
        if (fromPoint != null)
        {
            mapControl.Map?.Layers.Remove(
                fromLayer);
        }

        (
            MemoryLayer layer,
            GeoPoint point) =
            mapPinService.CreatePin(
                position,
                "From Pin",
                Mapsui.Styles.Color.Blue);

        fromLayer =
            layer;

        fromPoint =
            point;

        mapControl.Map?.Layers.Add(
            fromLayer);

        if (recalculate)
            RecalculateAccessibility();
    }

    private void SetToPin(
        MPoint position,
        bool recalculate = true)
    {
        if (toPoint != null)
        {
            mapControl.Map?.Layers.Remove(
                toLayer);
        }

        (
            MemoryLayer layer,
            GeoPoint point) =
            mapPinService.CreatePin(
                position,
                "To Pin",
                Mapsui.Styles.Color.Red);

        toLayer =
            layer;

        toPoint =
            point;

        mapControl.Map?.Layers.Add(
            toLayer);

        if (recalculate)
            RecalculateAccessibility();
    }

    public void FindRoutes(
    LugarLang.Internal.Models.Place from,
    LugarLang.Internal.Models.Place to)
    {
        SetFromPin(
            ProjectGeoPoint(
                new GeoPoint
                {
                    Latitude = from.Latitude,
                    Longitude = from.Longitude
                }),
            recalculate: false);

        SetToPin(
            ProjectGeoPoint(
                new GeoPoint
                {
                    Latitude = to.Latitude,
                    Longitude = to.Longitude
                }),
            recalculate: false);

        RecalculateAccessibility();
    }

    private MPoint ProjectGeoPoint(GeoPoint point)
    {
        var projected =
            Mapsui.Projections.SphericalMercator.FromLonLat(
                point.Longitude,
                point.Latitude);

        return new MPoint(projected.x, projected.y);
    }

    private MemoryLayer? fromLayer;
    private MemoryLayer? toLayer;

    private void RecalculateAccessibility()
    {
        tripVisualizationService.RemoveTripLayers(
            mapControl);

        if (
            fromPoint == null ||
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
            $"ROUTING TEST 3: Evaluated " +
            $"{currentCandidates.Count} direct candidates.");

        currentJourneys =
                segmentGraphJourneyRoutingService.FindJourneys(
                transitDataService.Network.Routes,
                fromPoint,
                toPoint,
                maximumWalkingDistanceMeters);

        System.Diagnostics.Debug.WriteLine(
            $"ROUTING TEST 4: Found " +
            $"{currentJourneys.Count} journeys.");

        routingCandidateView.DisplayJourneys(
            currentJourneys,
            maximumWalkingDistanceMeters);

        System.Diagnostics.Debug.WriteLine(
            "ROUTING TEST 5: Journey candidates displayed.");
    }

    private void RoutingCandidateView_CandidateSelected(
    object? sender,
    RoutingDebugInfo candidate)
    {
        System.Diagnostics.Debug.WriteLine(
            $"CANDIDATE SELECTED: {candidate.JourneyId}");

        Journey? selectedJourney =
            currentJourneys.FirstOrDefault(
                journey =>
                    BuildJourneyId(journey) ==
                    candidate.JourneyId);

        if (selectedJourney == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "CANDIDATE SELECTED: No matching journey found.");

            return;
        }

        System.Diagnostics.Debug.WriteLine(
            $"CANDIDATE SELECTED: Matching journey found with " +
            $"{selectedJourney.Legs.Count} legs.");

        DrawJourney(
            selectedJourney);
    }

    private void DrawJourney(
    Journey journey)
    {
        System.Diagnostics.Debug.WriteLine(
            "DRAW JOURNEY: Starting.");

        // Previously this looped routeDisplayService.DisplayRoute per leg,
        // which only draws each route's terminal stops (and, since
        // RouteDisplayService tracks a single stopLayer field, each call
        // wiped the previous leg's layer -- multi-leg journeys only ever
        // showed the last leg's stops). tripVisualizationService.DrawJourney
        // is the method actually built to draw the sliced ride polyline
        // per leg plus the dashed walking connections between them.
        tripVisualizationService.DrawJourney(
            mapControl,
            journey);

        System.Diagnostics.Debug.WriteLine(
            "DRAW JOURNEY: Completed.");
    }


    private void DisplayTripRoute(
    Route route)
    {
        routeDisplayService.DisplayRoute(
            mapControl,
            route,
            fromPoint,
            toPoint);
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
        routeDisplayService.DisplayRoute(
            mapControl,
            route,
            fromPoint,
            toPoint);
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

    private string BuildJourneyId(
    Journey journey)
    {
        return string.Join(
            "→",
            journey.Legs.Select(
                leg =>
                    $"{leg.RouteId}:{leg.DirectionName}"));
    }

    private void BuildResponsiveLayout()
    {
        double layoutWidth = Width;

        if (layoutWidth <= 0)
        {
            double density = DeviceDisplay.MainDisplayInfo.Density;
            layoutWidth = density > 0
                ? DeviceDisplay.MainDisplayInfo.Width / density
                : 390;
        }

        Content =
            layoutBuilder.Build(
                mapControl,
                routingCandidateView,
                routePicker,
                walkingDistancePicker,
                fromButton,
                toButton,
                layoutWidth);
    }
}
