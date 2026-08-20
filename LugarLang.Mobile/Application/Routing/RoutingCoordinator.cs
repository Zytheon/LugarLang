using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.Application.Routing;

public class RoutingCoordinator
{
    private readonly TripRoutingService tripRoutingService;
    private readonly RoutingVisualizationService routingVisualizationService;

    public RoutingCoordinator(
        TripRoutingService tripRoutingService,
        RoutingVisualizationService routingVisualizationService)
    {
        this.tripRoutingService =
            tripRoutingService;

        this.routingVisualizationService =
            routingVisualizationService;
    }

    public RoutingResult Calculate(
        IList<Route> routes,
        GeoPoint fromPoint,
        GeoPoint toPoint,
        double maximumWalkingDistanceMeters)
    {
        List<DirectionEvaluation> candidates =
            tripRoutingService.EvaluateAllTrips(
                routes,
                fromPoint,
                toPoint,
                maximumWalkingDistanceMeters);

        DirectionEvaluation? bestTrip =
            tripRoutingService.SelectBestTrip(
                routes,
                fromPoint,
                toPoint,
                maximumWalkingDistanceMeters);

        RoutingDebugSnapshot snapshot =
            routingVisualizationService.CreateSnapshot(
                candidates,
                bestTrip,
                maximumWalkingDistanceMeters);

        return new RoutingResult(
            candidates,
            bestTrip,
            snapshot);
    }
}