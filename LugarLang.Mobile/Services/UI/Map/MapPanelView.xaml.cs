using CdoGtfsConverter.Models;
using LugarLang.Internal.Pages;
using LugarLang.Internal.Services.Content;
using LugarLang.Mobile.Application.Routing;
using LugarLang.Mobile.Services.Mapping;
using LugarLang.Mobile.Services.Mapping.Factories;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingSpatial;
using LugarLang.Mobile.Services.RoutingVisualization;
using LugarLang.Mobile.Services.Transit;
using LugarLang.Mobile.Services.UI.Layout;
using LugarLang.Mobile.UI.Routing;
using Microsoft.Maui.Controls.Shapes;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;
using PlaceModel = LugarLang.Internal.Models.Place;

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
    private readonly JourneyDetailView journeyDetailView;
    private readonly Grid bottomHost;
    private readonly Grid mapHost;
    private readonly MapPlacePreviewView placePreviewView;
    private readonly JourneyPhotoMarkerService journeyPhotoMarkerService;

    private MemoryLayer? journeyPhotoLayer;

    private Dictionary<IFeature, JourneyPhotoMarker> journeyMarkerLookup = new();
    private readonly PlaceContentService placeContentService;

    private Picker routePicker = null!;
    private Picker walkingDistancePicker = null!;

private Label fromNameLabel = null!;
private Label toNameLabel = null!;
private Border startPinBorder = null!;
private Border endPinBorder = null!;
private Border pinHintBorder = null!;
private Label pinHintLabel = null!;

private enum PinTarget { None, Start, End }
private PinTarget activePinTarget = PinTarget.None;
    private string fromLabel = "";
    private string toLabel = "";
    private int fitTicket;

    private MemoryLayer? inboundLayer;
    private MemoryLayer? outboundLayer;
    private double maximumWalkingDistanceMeters = 500;

    private GeoPoint? fromPoint;
    private GeoPoint? toPoint;

    private List<DirectionEvaluation> currentCandidates = new();

    private List<Journey> currentJourneys = new();
    private PlaceModel? searchFromPlace;
    private PlaceModel? searchToPlace;

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

        placeContentService =
    IPlatformApplication.Current?.Services
        .GetService<PlaceContentService>()
    ?? new PlaceContentService();

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
        journeyDetailView =
            new JourneyDetailView
            {
                IsVisible = false
            };

        journeyDetailView.BackRequested +=
            (_, _) => ShowResults();

        journeyDetailView.SakayKoAniRequested +=
            async (_, _) =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlertAsync(
                        "Sakay ko ani",
                        "Coming soon: this will let the driver know you're waiting.",
                        "OK");
            };

        journeyDetailView.LandmarkDetailsRequested +=
    async (_, place) =>
    {
        if (Shell.Current != null)
            await Shell.Current.Navigation.PushAsync(
                new PlaceDetailsPage(place));
    };

        journeyDetailView.PlaceSelected +=
    (_, place) =>
    {
        ShowPlaceOnMap(
            place,
            "Selected from journey");

        FitMapToPlace(place);
    };

        bottomHost =
            new Grid();

        bottomHost.Children.Add(routingCandidateView);
        bottomHost.Children.Add(journeyDetailView);

        mapControl =
            new MapControl();

        journeyPhotoMarkerService =
    new JourneyPhotoMarkerService();

        mapHost = new Grid();

        mapHost.Children.Add(mapControl);

        placePreviewView =
            new MapPlacePreviewView();

        placePreviewView.CloseRequested +=
            (_, _) => placePreviewView.Hide();

        placePreviewView.DetailsRequested +=
            async (_, place) =>
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.Navigation.PushAsync(
                        new PlaceDetailsPage(place));
                }
            };

        mapHost.Children.Add(placePreviewView);

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
                TextColor = Theme.TextPrimary,
                TitleColor = Theme.TextSecondary,
                WidthRequest = 190,
                HorizontalOptions = LayoutOptions.Start
            };

        walkingDistancePicker.Items.Add("Walk up to 250 m");
        walkingDistancePicker.Items.Add("Walk up to 500 m");
        walkingDistancePicker.Items.Add("Walk up to 750 m");
        walkingDistancePicker.Items.Add("Walk up to 1 km");

        walkingDistancePicker.SelectedIndex = 1;

        walkingDistancePicker.SelectedIndexChanged +=
            WalkingDistancePicker_SelectedIndexChanged;

        fromNameLabel = CreateFieldNameLabel();
        toNameLabel = CreateFieldNameLabel();

        pinHintLabel =
            new Label
            {
                FontSize = 12,
                TextColor = Theme.Deep
            };

        pinHintBorder =
            new Border
            {
                IsVisible = false,
                Padding = new Thickness(10, 6),
                StrokeThickness = 0,
                BackgroundColor = Theme.Secondary,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Content = pinHintLabel
            };

        mapControl.Info +=
            MapControl_Info;

        mapControl.SizeChanged +=
            (_, _) =>
            {
                if (pendingFitBox != null)
                    RequestPendingFit();
            };

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
        // First check whether the tap hit one of our journey photo markers.
        if (journeyPhotoLayer != null &&
            journeyMarkerLookup.Count > 0)
        {
            Mapsui.MapInfo mapInfo =
                e.GetMapInfo(
                    new ILayer[] { journeyPhotoLayer });

            if (mapInfo.Feature != null &&
                journeyMarkerLookup.TryGetValue(
                    mapInfo.Feature,
                    out JourneyPhotoMarker? marker))
            {
                e.Handled = true;

                ShowPlaceOnMap(
                    marker.Place,
                    marker.Caption);

                FitMapToPlace(marker.Place);

                return;
            }
        }

        // Only unhandled taps continue into the existing pin-placement logic.
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
            fromLabel = "Dropped pin";
            searchFromPlace = null;

            SetFromPin(e.WorldPosition);
            SetActivePin(PinTarget.None);

            return;
        }

        if (
            mode ==
            MapInteractionController.PinMode.SettingTo)
        {
            toLabel = "Dropped pin";
            searchToPlace = null;

            SetToPin(e.WorldPosition);
            SetActivePin(PinTarget.None);
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

        UpdateEndpointButtons();

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

        UpdateEndpointButtons();

        if (recalculate)
            RecalculateAccessibility();
    }

    public void FindRoutes(
    LugarLang.Internal.Models.Place from,
    LugarLang.Internal.Models.Place to)
    {
        mapInteractionController.Cancel();
        SetActivePin(PinTarget.None);
        fromLabel = from.Name;
        toLabel = to.Name;
        searchFromPlace = from;
        searchToPlace = to;

        ScheduleFit(
    BuildPinsBox(
        ProjectGeoPoint(
            new GeoPoint
            {
                Latitude = from.Latitude,
                Longitude = from.Longitude
            }),
        ProjectGeoPoint(
            new GeoPoint
            {
                Latitude = to.Latitude,
                Longitude = to.Longitude
            })));

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
    private MRect? pendingFitBox;

    private void RecalculateAccessibility()
    {
        ShowResults();

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

        bool wasInDetail = journeyDetailView.IsVisible;

        DrawJourney(
            selectedJourney);

        ShowJourney(candidate, selectedJourney);

        FitMapToJourney(
            selectedJourney,
            layoutWillChange: !wasInDetail);
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
            double density =
                DeviceDisplay.MainDisplayInfo.Density;

            layoutWidth = density > 0
                ? DeviceDisplay.MainDisplayInfo.Width / density
                : 390;
        }

        Content =
            layoutBuilder.Build(
                mapHost,
                bottomHost,
                BuildControlsBar(),
                layoutWidth);
    }


    private (double Endpoint, double Landmark)
        GetJourneyMarkerSizes()
    {
        double mapWidth = mapControl.Width;

        // Fallback if the map hasn't completed layout yet.
        if (mapWidth <= 0)
            mapWidth = Width;

        if (mapWidth <= 0)
        {
            double density =
                DeviceDisplay.MainDisplayInfo.Density;

            mapWidth = density > 0
                ? DeviceDisplay.MainDisplayInfo.Width / density
                : 390;
        }

        // Desktop
        if (mapWidth >= 1000)
            return (88, 68);

        // Tablet
        if (mapWidth >= 600)
            return (76, 58);

        // Phone
        return (68, 52);
    }


    private void ShowJourney(RoutingDebugInfo candidate, Journey journey)
    {
        HashSet<string> used = new();
        // The start and end places have their own rows, so they must not
        // also appear as landmarks.
        if (searchFromPlace != null) used.Add(searchFromPlace.Id);
        if (searchToPlace != null) used.Add(searchToPlace.Id);
        List<List<PlaceModel>> landmarksPerLeg = new();

        for (int i = 0; i < journey.Legs.Count; i++)
        {
            // A landmark shows once per journey, on the first ride that passes it.
            List<PlaceModel> list =
                FindLandmarks(journey.Legs[i])
                    .Where(place => used.Add(place.Id))
                    .ToList();

            System.Diagnostics.Debug.WriteLine(
                $"LANDMARKS leg {i} ({journey.Legs[i].RouteName}): {list.Count}");

            landmarksPerLeg.Add(list);
        }

        ShowJourneyPhotoMarkers(landmarksPerLeg, searchFromPlace, searchToPlace);

        journeyDetailView.Show(candidate, landmarksPerLeg, searchFromPlace, searchToPlace);

        routingCandidateView.IsVisible = false;
        journeyDetailView.IsVisible = true;

        layoutBuilder.SetDetailMode(true);
    }

    private const double LandmarkMaxDistanceMeters = 150;

    // TEMPORARY: every place counts as a landmark here, not only the ones
    // switched on with IsLandmark in the place editor. Hard-coded for now
    // while we decide how landmarks should really be chosen. Set this to
    // false to go back to only the places flagged in the editor.
    private const bool TreatAllPlacesAsLandmarks = true;

    private const int MaxLandmarksPerRide = 4;

    // Landmark places within 75 m of the part of the route the rider
    // actually rides, in the order they are passed.
    private List<PlaceModel> FindLandmarks(JourneyLeg leg)
    {
        DirectionEvaluation e = leg.Evaluation;

        int from = Math.Max(0, e.FromIndex);
        int to = Math.Min(e.ToIndex, e.Direction.Path.Count - 1);

        if (to <= from)
            return new List<PlaceModel>();

        List<GeoPoint> slice =
            e.Direction.Path.GetRange(from, to - from + 1);

        List<(PlaceModel Place, int Order, double Distance)> found = new();

        System.Diagnostics.Debug.WriteLine(
    $"LANDMARKS: {placeContentService.GetAllPlaces().Count(p => p.IsLandmark)} flagged place(s) in memory");

        foreach (PlaceModel place in placeContentService.GetAllPlaces())
        {
            if (!TreatAllPlacesAsLandmarks && !place.IsLandmark)
                continue;

            RouteAccessResult access =
                routeAccessibilityService.FindNearestPoint(
                    place.Latitude,
                    place.Longitude,
                    slice);

            System.Diagnostics.Debug.WriteLine(
    $"LANDMARK CHECK: {place.Name} is {access.DistanceMeters:F0} m from the ridden line (limit {LandmarkMaxDistanceMeters} m)");

            if (access.NearestPoint == null ||
                access.DistanceMeters > LandmarkMaxDistanceMeters)
            {
                continue;
            }

            // Position along the ride: the slice point closest to the
            // nearest point on the line.
            int order = 0;
            double best = double.MaxValue;

            for (int i = 0; i < slice.Count; i++)
            {
                double dLat = slice[i].Latitude - access.NearestPoint.Latitude;
                double dLon = slice[i].Longitude - access.NearestPoint.Longitude;
                double d = dLat * dLat + dLon * dLon;

                if (d < best)
                {
                    best = d;
                    order = i;
                }
            }

            found.Add((place, order, access.DistanceMeters));
        }

        // Keep the best few (photos first, then closest), then list them
        // in riding order.
        return found
            .OrderByDescending(f => f.Place.IsLandmark)
            .ThenByDescending(f => f.Place.Photos.Count > 0)
            .ThenBy(f => f.Distance)
            .Take(MaxLandmarksPerRide)
            .OrderBy(f => f.Order)
            .Select(f => f.Place)
            .ToList();
    }

    private void ShowResults()
    {
        ClearJourneyPhotoMarkers();

        journeyDetailView.IsVisible = false;
        routingCandidateView.IsVisible = true;

        layoutBuilder.SetDetailMode(false);
    }

    // Zooms the map to the whole journey. When the map is about to be
    // resized (list -> journey), the zoom is applied once the new size
    // arrives, otherwise it would be computed for the old, smaller map.
    private void FitMapToJourney(
        Journey journey,
        bool layoutWillChange)
    {
        MRect? box = BuildJourneyBox(journey);

        if (box == null)
            return;

        if (!layoutWillChange)
        {
            mapControl.Map?.Navigator.ZoomToBox(box);
            return;
        }

        ScheduleFit(box);
    }

    private void ApplyPendingFit()
    {
        if (pendingFitBox == null)
            return;

        // Not laid out yet: keep the box, the next SizeChanged retries.
        if (mapControl.Width <= 0 || mapControl.Height <= 0)
            return;


        MRect box = pendingFitBox;
        pendingFitBox = null;

        mapControl.Map?.Navigator.ZoomToBox(box);
    }

    private MRect? BuildJourneyBox(Journey journey)
    {
        List<MPoint> points = new();

        foreach (JourneyLeg leg in journey.Legs)
        {
            DirectionEvaluation e = leg.Evaluation;

            points.Add(ProjectGeoPoint(e.From));
            points.Add(ProjectGeoPoint(e.NearestFrom));

            for (
                int i = e.FromIndex;
                i <= e.ToIndex && i < e.Direction.Path.Count;
                i++)
            {
                if (i < 0)
                    continue;

                points.Add(ProjectGeoPoint(e.Direction.Path[i]));
            }

            points.Add(ProjectGeoPoint(e.NearestTo));
            points.Add(ProjectGeoPoint(e.To));
        }

        if (points.Count == 0)
            return null;

        double minX = points.Min(p => p.X);
        double maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxY = points.Max(p => p.Y);

        // 15% breathing room, with a floor so a very short trip
        // doesn't zoom in absurdly far.
        double padX = Math.Max((maxX - minX) * 0.15, 150);
        double padY = Math.Max((maxY - minY) * 0.15, 150);

        return new MRect(
            minX - padX,
            minY - padY,
            maxX + padX,
            maxY + padY);
    }

    // Waits until the map has stopped resizing (the sheet animates its
    // height when it opens) and then zooms once. Every resize restarts
    // the wait, so a half-animated size is never used.
    private void ScheduleFit(MRect box)
    {
        pendingFitBox = box;
        RequestPendingFit();
    }

    private void RequestPendingFit()
    {
        int ticket = ++fitTicket;

        Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(200),
            () =>
            {
                if (ticket == fitTicket)
                    ApplyPendingFit();
            });
    }

    private static MRect BuildPinsBox(MPoint a, MPoint b)
    {
        double minX = Math.Min(a.X, b.X);
        double maxX = Math.Max(a.X, b.X);
        double minY = Math.Min(a.Y, b.Y);
        double maxY = Math.Max(a.Y, b.Y);

        // 25% breathing room, with a floor for pins that are very close.
        double padX = Math.Max((maxX - minX) * 0.25, 300);
        double padY = Math.Max((maxY - minY) * 0.25, 300);

        return new MRect(
            minX - padX,
            minY - padY,
            maxX + padX,
            maxY + padY);
    }

    private void UpdateEndpointButtons()
    {
        fromNameLabel.Text =
            fromPoint == null ? "Not set" : fromLabel;

        toNameLabel.Text =
            toPoint == null ? "Not set" : toLabel;
    }

    private static Label CreateFieldNameLabel()
    {
        return new Label
        {
            Text = "Not set",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Theme.TextPrimary,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
    }

    // A read-only box showing the current start or end place.
    private static View CreateEndpointField(
        string caption,
        Label nameLabel)
    {
        return new Border
        {
            HeightRequest = 48,
            Padding = new Thickness(10, 0),
            Stroke = Theme.Border,
            StrokeThickness = 1,
            BackgroundColor = Theme.Surface,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 0,
                Children =
            {
                new Label
                {
                    Text = caption,
                    FontSize = 10,
                    TextColor = Theme.TextSecondary
                },
                nameLabel
            }
            }
        };
    }

    private void SetActivePin(PinTarget target)
    {
        activePinTarget = target;

        startPinBorder.StrokeThickness = target == PinTarget.Start ? 3 : 0;
        endPinBorder.StrokeThickness = target == PinTarget.End ? 3 : 0;

        pinHintBorder.IsVisible = target != PinTarget.None;

        pinHintLabel.Text =
            target == PinTarget.Start
                ? "Tap the map to drop the Start pin."
                : "Tap the map to drop the End pin.";
    }

    // A circular button with a pin icon and a caption under it.
    private static View CreatePinControl(
        Color color,
        string caption,
        Action onTap,
        out Border circle)
    {
        circle =
            new Border
            {
                WidthRequest = 48,
                HeightRequest = 48,
                Stroke = Theme.Primary,
                StrokeThickness = 0,
                BackgroundColor = color,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new GraphicsView
                {
                    Drawable = new PinIconDrawable(color),
                    WidthRequest = 24,
                    HeightRequest = 24,
                    InputTransparent = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };

        TapGestureRecognizer tap = new();
        tap.Tapped += (_, _) => onTap();
        circle.GestureRecognizers.Add(tap);

        SemanticProperties.SetDescription(circle, caption);

        return new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Start,
            Children =
        {
            circle,
            new Label
            {
                Text = caption,
                FontSize = 10,
                TextColor = Theme.TextSecondary,
                HorizontalTextAlignment = TextAlignment.Center
            }
        }
        };
    }

    private sealed class PinIconDrawable : IDrawable
    {
        private readonly Color holeColor;

        public PinIconDrawable(Color holeColor)
        {
            this.holeColor = holeColor;
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            float s = rect.Width / 24f;

            PathF pin = new();
            pin.MoveTo(12 * s, 2 * s);
            pin.CurveTo(8.13f * s, 2 * s, 5 * s, 5.13f * s, 5 * s, 9 * s);
            pin.CurveTo(5 * s, 14.25f * s, 12 * s, 22 * s, 12 * s, 22 * s);
            pin.CurveTo(12 * s, 22 * s, 19 * s, 14.25f * s, 19 * s, 9 * s);
            pin.CurveTo(19 * s, 5.13f * s, 15.87f * s, 2 * s, 12 * s, 2 * s);
            pin.Close();

            canvas.FillColor = Colors.White;
            canvas.FillPath(pin);

            canvas.FillColor = holeColor;
            canvas.FillCircle(12 * s, 9 * s, 3.2f * s);
        }
    }

    private Microsoft.Maui.Controls.View BuildControlsBar()
    {

        View startPin =
    CreatePinControl(
        Theme.StartPin,
        "Start pin",
        () =>
        {
            if (activePinTarget == PinTarget.Start)
            {
                mapInteractionController.Cancel();
                SetActivePin(PinTarget.None);
                return;
            }

            mapInteractionController.StartSettingFrom();
            SetActivePin(PinTarget.Start);
        },
        out startPinBorder);

        View endPin =
            CreatePinControl(
                Theme.EndPin,
                "End pin",
                () =>
                {
                    if (activePinTarget == PinTarget.End)
                    {
                        mapInteractionController.Cancel();
                        SetActivePin(PinTarget.None);
                        return;
                    }

                    mapInteractionController.StartSettingTo();
                    SetActivePin(PinTarget.End);
                },
                out endPinBorder);

        Label arrow =
            new()
            {
                Text = "→",
                FontSize = 16,
                TextColor = Theme.TextSecondary,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 12, 0, 0)
            };

        View fromField = CreateEndpointField("Start", fromNameLabel);
        View toField = CreateEndpointField("End", toNameLabel);

        Grid endpoints =
            new()
            {
                ColumnSpacing = 6,
                ColumnDefinitions =
                {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
                }
            };

        Grid.SetColumn(fromField, 0);
        Grid.SetColumn(arrow, 1);
        Grid.SetColumn(toField, 2);
        Grid.SetColumn(startPin, 3);
        Grid.SetColumn(endPin, 4);

        endpoints.Children.Add(fromField);
        endpoints.Children.Add(arrow);
        endpoints.Children.Add(toField);
        endpoints.Children.Add(startPin);
        endpoints.Children.Add(endPin);

        Grid bar =
            new()
            {
                Padding = new Thickness(12, 8),
                RowSpacing = 6,
                BackgroundColor = Theme.PageBackground,
                RowDefinitions =
                {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
                }
            };

        Grid.SetRow(pinHintBorder, 1);
        Grid.SetRow(walkingDistancePicker, 2);

        bar.Children.Add(endpoints);
        bar.Children.Add(pinHintBorder);
        bar.Children.Add(walkingDistancePicker);

#if DEV
    // The all-routes picker is a debugging tool, so it stays in dev builds.
    bar.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
    Grid.SetRow(routePicker, 3);
    bar.Children.Add(routePicker);
#endif

        return bar;
    }

    
    private void ShowJourneyPhotoMarkers(
        List<List<PlaceModel>> landmarksPerLeg,
        PlaceModel? startPlace,
        PlaceModel? endPlace)
    {
        ClearJourneyPhotoMarkers();

        List<JourneyPhotoMarker> markers = new();
        HashSet<string> used = new();

        System.Diagnostics.Debug.WriteLine(
            "========== JOURNEY PHOTO MARKERS ==========");

        System.Diagnostics.Debug.WriteLine(
            $"START: {startPlace?.Name ?? "(null)"}");

        System.Diagnostics.Debug.WriteLine(
            $"END: {endPlace?.Name ?? "(null)"}");

        System.Diagnostics.Debug.WriteLine(
            $"LEGS: {landmarksPerLeg.Count}");

        if (startPlace != null && used.Add(startPlace.Id))
        {
            markers.Add(new JourneyPhotoMarker
            {
                Place = startPlace,
                Caption = "Journey start",
                IsEndpoint = true
            });
        }

        foreach (List<PlaceModel> legLandmarks in landmarksPerLeg)
        {
            foreach (PlaceModel place in legLandmarks)
            {
                if (!used.Add(place.Id))
                    continue;

                markers.Add(new JourneyPhotoMarker
                {
                    Place = place,
                    Caption = "Journey landmark",
                    IsEndpoint = false
                });
            }
        }

        if (endPlace != null && used.Add(endPlace.Id))
        {
            markers.Add(new JourneyPhotoMarker
            {
                Place = endPlace,
                Caption = "Journey destination",
                IsEndpoint = true
            });
        }

        System.Diagnostics.Debug.WriteLine(
            $"TOTAL MARKERS: {markers.Count}");

        foreach (JourneyPhotoMarker marker in markers)
        {
            PlaceModel place = marker.Place;

            System.Diagnostics.Debug.WriteLine(
                $"MARKER: {place.Name} | " +
                $"Lat={place.Latitude} | " +
                $"Lon={place.Longitude} | " +
                $"Photos={place.Photos?.Count ?? 0} | " +
                $"Endpoint={marker.IsEndpoint}");

            if (place.Photos != null)
            {
                foreach (string path in place.Photos)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"  PHOTO: {path} | Exists={File.Exists(path)}");
                }
            }
        }

        if (markers.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine(
                "NO MARKERS GENERATED.");

            System.Diagnostics.Debug.WriteLine(
                "==========================================");

            return;
        }

        // Determine marker dimensions from the actual map width.
        (double endpointSize, double landmarkSize) =
            GetJourneyMarkerSizes();

        System.Diagnostics.Debug.WriteLine(
            $"[PHOTO MARKER] Map width: {mapControl.Width:F0}");

        System.Diagnostics.Debug.WriteLine(
            $"[PHOTO MARKER] Endpoint size: {endpointSize}");

        System.Diagnostics.Debug.WriteLine(
            $"[PHOTO MARKER] Landmark size: {landmarkSize}");

        var result =
            journeyPhotoMarkerService.CreateLayer(
                markers,
                endpointSize,
                landmarkSize);

        journeyPhotoLayer = result.Layer;
        journeyMarkerLookup = result.MarkerLookup;

        System.Diagnostics.Debug.WriteLine(
            $"FEATURE COUNT: {journeyPhotoLayer.Features.Count()}");

        System.Diagnostics.Debug.WriteLine(
            $"LOOKUP COUNT: {journeyMarkerLookup.Count}");

        System.Diagnostics.Debug.WriteLine(
            $"MAP EXISTS: {mapControl.Map != null}");

        if (mapControl.Map != null)
        {
            mapControl.Map.Layers.Add(journeyPhotoLayer);

            System.Diagnostics.Debug.WriteLine(
                $"LAYER ADDED. MAP LAYERS: {mapControl.Map.Layers.Count}");
        }

        System.Diagnostics.Debug.WriteLine(
            "==========================================");
    }



    private void ClearJourneyPhotoMarkers()
    {
        if (journeyPhotoLayer != null)
        {
            mapControl.Map?.Layers.Remove(journeyPhotoLayer);
            journeyPhotoLayer = null;
        }

        journeyMarkerLookup.Clear();

        placePreviewView.Hide();
    }

    private void ShowPlaceOnMap(
        PlaceModel place,
        string caption)
    {
        placePreviewView.Show(place, caption);
    }

    private void FitMapToPlace(PlaceModel place)
    {
        MPoint position =
            ProjectGeoPoint(
                new GeoPoint
                {
                    Latitude = place.Latitude,
                    Longitude = place.Longitude
                });

        double padding = 250;

        mapControl.Map?.Navigator.ZoomToBox(
            new MRect(
                position.X - padding,
                position.Y - padding,
                position.X + padding,
                position.Y + padding));
    }
}
