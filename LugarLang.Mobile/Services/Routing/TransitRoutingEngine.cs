using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;

namespace LugarLang.Mobile.Services.Routing;

public class TransitRoutingEngine
{
    private readonly RouteAccessibilityService routeAccessibilityService;

    public TransitRoutingEngine(
        RouteAccessibilityService routeAccessibilityService)
    {
        this.routeAccessibilityService =
            routeAccessibilityService;
    }

    public List<TransitRouteState> ExpandOneTransfer(
    IEnumerable<TransitRouteState> currentStates,
    IEnumerable<Route> routes,
    double maximumTransferWalkingDistanceMeters)
    {
        List<TransitRouteState> nextStates =
            new();

        foreach (
            TransitRouteState currentState
            in currentStates)
        {
            foreach (
                Route route
                in routes)
            {
                foreach (
                    Direction direction
                    in GetDirections(route))
                {
                    if (
                        route.Id ==
                        currentState.Route.Id &&
                        direction ==
                        currentState.Direction)
                    {
                        continue;
                    }

                    RouteAccessResult transfer =
                        FindTransferFromState(
                            currentState,
                            direction,
                            maximumTransferWalkingDistanceMeters);

                    if (
                        transfer.NearestPoint == null)
                    {
                        continue;
                    }

                    int transferIndex =
                        FindNearestPathIndex(
                            transfer.NearestPoint,
                            direction.Path);

                    if (
                        transferIndex >=
                        direction.Path.Count - 1)
                    {
                        continue;
                    }

                    double rideDistance =
                        CalculatePathDistance(
                            currentState.Direction.Path,
                            currentState.PathIndex,
                            currentState.Direction.Path.Count - 1);

                    if (rideDistance <= 0)
                    {
                        continue;
                    }

                    nextStates.Add(
                        new TransitRouteState
                        {
                            Route =
                                route,

                            Direction =
                                direction,

                            PathIndex =
                                transferIndex,

                            WalkingDistanceMeters =
                                currentState.WalkingDistanceMeters +
                                transfer.DistanceMeters,

                            RideDistanceMeters =
                                currentState.RideDistanceMeters +
                                rideDistance,

                            NumberOfTransfers =
                                currentState.NumberOfTransfers +
                                1,

                            PreviousState =
                                currentState
                        });
                }
            }
        }

        return RemoveDominatedStates(
            nextStates);
    }

    public List<TransitRouteState> FindInitialStates(
        IEnumerable<Route> routes,
        GeoPoint from,
        double maximumWalkingDistanceMeters)
    {
        List<TransitRouteState> states =
            new();

        foreach (
            Route route
            in routes)
        {
            foreach (
                Direction direction
                in GetDirections(route))
            {
                if (
                    direction.Path == null ||
                    direction.Path.Count == 0)
                {
                    continue;
                }

                RouteAccessResult access =
                    routeAccessibilityService.FindNearestPoint(
                        from.Latitude,
                        from.Longitude,
                        direction.Path);

                if (
                    access.NearestPoint == null)
                {
                    continue;
                }

                if (
                    access.DistanceMeters >
                    maximumWalkingDistanceMeters)
                {
                    continue;
                }

                int pathIndex =
                    FindNearestPathIndex(
                        access.NearestPoint,
                        direction.Path);

                states.Add(
                    new TransitRouteState
                    {
                        Route =
                            route,

                        Direction =
                            direction,

                        PathIndex =
                            pathIndex,

                        WalkingDistanceMeters =
                            access.DistanceMeters,

                        RideDistanceMeters =
                            0,

                        NumberOfTransfers =
                            0,

                        PreviousState =
                            null
                    });
            }
        }

        return RemoveDuplicateStates(
            states);
    }

    private IEnumerable<Direction> GetDirections(
        Route route)
    {
        if (route.Inbound != null)
        {
            yield return route.Inbound;
        }

        if (route.Outbound != null)
        {
            yield return route.Outbound;
        }
    }

    private int FindNearestPathIndex(
        GeoPoint point,
        IList<GeoPoint> path)
    {
        double smallestDistance =
            double.MaxValue;

        int nearestIndex = 0;

        for (
            int i = 0;
            i < path.Count;
            i++)
        {
            double distance =
                CalculateDistanceMeters(
                    point.Latitude,
                    point.Longitude,
                    path[i].Latitude,
                    path[i].Longitude);

            if (
                distance <
                smallestDistance)
            {
                smallestDistance =
                    distance;

                nearestIndex =
                    i;
            }
        }

        return nearestIndex;
    }

    private RouteAccessResult FindTransferFromState(
    TransitRouteState currentState,
    Direction targetDirection,
    double maximumTransferWalkingDistanceMeters)
    {
        double bestDistance =
            double.MaxValue;

        GeoPoint? bestPoint =
            null;

        IList<GeoPoint> currentPath =
            currentState.Direction.Path;

        for (
            int i = currentState.PathIndex;
            i < currentPath.Count;
            i++)
        {
            GeoPoint point =
                currentPath[i];

            RouteAccessResult result =
                routeAccessibilityService.FindNearestPoint(
                    point.Latitude,
                    point.Longitude,
                    targetDirection.Path);

            if (
                result.NearestPoint == null)
            {
                continue;
            }

            if (
                result.DistanceMeters >
                maximumTransferWalkingDistanceMeters)
            {
                continue;
            }

            if (
                result.DistanceMeters <
                bestDistance)
            {
                bestDistance =
                    result.DistanceMeters;

                bestPoint =
                    result.NearestPoint;
            }
        }

        return new RouteAccessResult
        {
            DistanceMeters =
                bestDistance,

            NearestPoint =
                bestPoint
        };
    }

    private double CalculatePathDistance(
        IList<GeoPoint> path,
        int fromIndex,
        int toIndex)
    {
        if (
            path.Count < 2 ||
            fromIndex < 0 ||
            toIndex >= path.Count ||
            toIndex <= fromIndex)
        {
            return 0;
        }

        double totalDistance =
            0;

        for (
            int i = fromIndex;
            i < toIndex;
            i++)
        {
            totalDistance +=
                CalculateDistanceMeters(
                    path[i].Latitude,
                    path[i].Longitude,
                    path[i + 1].Latitude,
                    path[i + 1].Longitude);
        }

        return totalDistance;
    }

    private double CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadius =
            6371000.0;

        double lat1 =
            latitude1 *
            Math.PI /
            180.0;

        double lat2 =
            latitude2 *
            Math.PI /
            180.0;

        double deltaLat =
            (latitude2 - latitude1) *
            Math.PI /
            180.0;

        double deltaLon =
            (longitude2 - longitude1) *
            Math.PI /
            180.0;

        double a =
            Math.Sin(deltaLat / 2) *
            Math.Sin(deltaLat / 2) +

            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Sin(deltaLon / 2) *
            Math.Sin(deltaLon / 2);

        double c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private List<TransitRouteState>
    RemoveDominatedStates(
        List<TransitRouteState> states)
    {
        List<TransitRouteState> result =
            new();

        foreach (
            TransitRouteState state
            in states)
        {
            bool dominated =
                states.Any(
                    other =>
                        other != state &&

                        other.Route.Id ==
                        state.Route.Id &&

                        other.Direction ==
                        state.Direction &&

                        other.PathIndex >=
                        state.PathIndex &&

                        other.WalkingDistanceMeters <=
                        state.WalkingDistanceMeters &&

                        other.RideDistanceMeters <=
                        state.RideDistanceMeters);

            if (!dominated)
            {
                result.Add(state);
            }
        }

        return result;
    }

    private List<TransitRouteState>
        RemoveDuplicateStates(
            List<TransitRouteState> states)
    {
        List<TransitRouteState> result =
            new();

        foreach (
            TransitRouteState state
            in states)
        {
            bool duplicate =
                result.Any(
                    existing =>
                        existing.Route.Id ==
                        state.Route.Id &&

                        existing.Direction ==
                        state.Direction);

            if (!duplicate)
            {
                result.Add(
                    state);
            }
        }

        return result;
    }
}