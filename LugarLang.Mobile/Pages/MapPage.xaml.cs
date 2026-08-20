using CdoGtfsConverter.Models;
using LugarLang.Mobile.Application.Routing;
using LugarLang.Mobile.Services.Mapping;
using LugarLang.Mobile.Services.Mapping.Factories;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;
using LugarLang.Mobile.Services.Transit;
using LugarLang.Mobile.UI.Layout;
using LugarLang.Mobile.UI.Routing;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace LugarLang.Mobile.Pages;

public partial class MapPage : ContentPage
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
    private readonly JourneyRoutingService journeyRoutingService;
    private readonly RoutingCandidateService routingCandidateService;
    private readonly RoutingVisualizationService routingVisualizationService;
    private readonly RoutingCandidateView routingCandidateView;
    private readonly MapPageLayoutBuilder layoutBuilder;
    private readonly TripVisualizationService tripVisualizationService;

    private Picker routePicker = null!;
    private Picker walkingDistancePicker = null!;

    private Button fromButton = null!;
    private Button toButton = null!;

    private double maximumWalkingDistanceMeters = 500;

    private GeoPoint? fromPoint;
    private GeoPoint? toPoint;

    private List<DirectionEvaluation> currentCandidates = new();

    public MapPage()
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
            OpenStreetMap.CreateTileLayer());
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
        MPoint position)
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

        RecalculateAccessibility();
    }

    private void SetToPin(
        MPoint position)
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

        RecalculateAccessibility();
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

    private void DisplayTripRoute(
    Route route)
    {
        routeDisplayService.DisplayRoute(
            mapControl,
            route,
            fromPoint,
            toPoint);
    }

    private void DrawTrip(
        DirectionEvaluation evaluation)
    {
        System.Diagnostics.Debug.WriteLine(
            "DRAW TRIP: Starting.");

        tripVisualizationService.DrawTrip(
            mapControl,
            evaluation);

        System.Diagnostics.Debug.WriteLine(
            "DRAW TRIP: Completed.");
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

    private void RemoveCurrentRouteLayers()
    {
        routeDisplayService.RemoveCurrentRouteLayers(
            mapControl);
    }

    private void BuildResponsiveLayout()
    {
        Content =
            layoutBuilder.Build(
                mapControl,
                routingCandidateView,
                routePicker,
                walkingDistancePicker,
                fromButton,
                toButton,
                Width);
    }
}