using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;

namespace LugarLang.Mobile.Services.Routing;

public class JourneyRoutingService
{
    private readonly TripRoutingService tripRoutingService;
    private readonly RouteTransferService routeTransferService;
    private readonly RouteAccessibilityService routeAccessibilityService;

    private readonly Dictionary<Direction, RouteBounds>
    routeBoundsCache =
        new();

    private readonly record struct RouteBounds(
        double MinLatitude,
        double MaxLatitude,
        double MinLongitude,
        double MaxLongitude);

    public JourneyRoutingService(
        TripRoutingService tripRoutingService,
        RouteTransferService routeTransferService,
        RouteAccessibilityService routeAccessibilityService)


    {
        this.tripRoutingService =
            tripRoutingService;

        this.routeTransferService =
            routeTransferService;

        this.routeAccessibilityService =
            routeAccessibilityService;
    }

    public List<Journey> FindJourneys(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        List<Route> routeList =
            routes.ToList();

        List<Journey> journeys =
            new();

        AddDirectJourneys(
            journeys,
            routeList,
            from,
            to,
            maximumWalkingDistanceMeters);

        AddTwoRideJourneys(
            journeys,
            routeList,
            from,
            to,
            maximumWalkingDistanceMeters);

        return RemoveDuplicateJourneys(
            journeys);
    }

    public Journey? SelectBestJourney(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        List<Journey> journeys =
            FindJourneys(
                routes,
                from,
                to,
                maximumWalkingDistanceMeters);

        if (journeys.Count == 0)
        {
            return null;
        }

        return journeys
            .OrderBy(
                journey =>
                    journey.TotalWalkingDistanceMeters)
            .ThenBy(
                journey =>
                    journey.NumberOfTransfers)
            .ThenBy(
                journey =>
                    journey.TotalRideDistanceMeters)
            .First();
    }

    private void AddDirectJourneys(
        List<Journey> journeys,
        List<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        List<DirectionEvaluation> candidates =
            tripRoutingService.EvaluateAllTrips(
                routes,
                from,
                to,
                maximumWalkingDistanceMeters);

        foreach (
            DirectionEvaluation candidate
            in candidates)
        {
            if (!candidate.Viable)
            {
                continue;
            }

            Journey journey =
                new();

            journey.Legs.Add(
                new JourneyLeg
                {
                    Evaluation =
                        candidate
                });

            journeys.Add(
                journey);
        }
    }

    private void AddTwoRideJourneys(
    List<Journey> journeys,
    List<Route> routes,
    GeoPoint from,
    GeoPoint to,
    double maximumWalkingDistanceMeters)
    {
        List<(Route Route, Direction Direction, RouteAccessResult AccessResult)>
            originDirections =
                new();

        List<(Route Route, Direction Direction, RouteAccessResult AccessResult)>
            destinationDirections =
                new();

        foreach (Route route in routes)
        {
            foreach (Direction direction in GetDirections(route))
            {
                RouteAccessResult fromResult =
                    routeAccessibilityService.FindNearestPointIndexed(
                        direction,
                        from.Latitude,
                        from.Longitude,
                        maximumWalkingDistanceMeters);

                if (
                    fromResult.NearestPoint != null &&
                    fromResult.DistanceMeters <=
                    maximumWalkingDistanceMeters)
                {
                    originDirections.Add(
                        (route, direction, fromResult));
                }

                RouteAccessResult toResult =
                    routeAccessibilityService.FindNearestPointIndexed(
                        direction,
                        to.Latitude,
                        to.Longitude,
                        maximumWalkingDistanceMeters);

                if (
                    toResult.NearestPoint != null &&
                    toResult.DistanceMeters <=
                    maximumWalkingDistanceMeters)
                {
                    destinationDirections.Add(
                        (route, direction, toResult));
                }
            }
        }

        foreach (
            (Route firstRoute, Direction firstDirection, RouteAccessResult fromAccessResult)
            in originDirections)
        {
            foreach (
                (Route secondRoute, Direction secondDirection, RouteAccessResult toAccessResult)
                in destinationDirections)
            {
                if (
                    firstRoute == secondRoute &&
                    firstDirection == secondDirection)
                {
                    continue;
                }

                AddTwoRideJourneyCandidates(
                    journeys,
                    firstRoute,
                    firstDirection,
                    secondRoute,
                    secondDirection,
                    from,
                    to,
                    maximumWalkingDistanceMeters,
                    fromAccessResult,
                    toAccessResult);
            }
        }
    }

    private void AddTwoRideJourneyCandidates(
    List<Journey> journeys,
    Route firstRoute,
    Direction firstDirection,
    Route secondRoute,
    Direction secondDirection,
    GeoPoint from,
    GeoPoint to,
    double maximumWalkingDistanceMeters,
    RouteAccessResult fromAccessResult,
    RouteAccessResult toAccessResult)
    {
        if (
            fromAccessResult.NearestPoint == null ||
            toAccessResult.NearestPoint == null)
        {
            return;
        }

        if (
            fromAccessResult.DistanceMeters >
            maximumWalkingDistanceMeters)
        {
            return;
        }

        if (
            toAccessResult.DistanceMeters >
            maximumWalkingDistanceMeters)
        {
            return;
        }

        int fromIndex =
            FindNearestPathIndex(
                fromAccessResult.NearestPoint,
                firstDirection.Path);

        int toIndex =
            FindNearestPathIndex(
                toAccessResult.NearestPoint,
                secondDirection.Path);

        if (
            !CouldRoutesTransfer(
                firstDirection,
                secondDirection,
                maximumWalkingDistanceMeters))
        {
            return;
        }

        List<TransferPoint> transfers =
            routeTransferService.FindTransferPoints(
                firstDirection,
                secondDirection,
                maximumWalkingDistanceMeters);

        if (transfers.Count == 0)
        {
            return;
        }

        foreach (
            TransferPoint transfer
            in transfers)
        {
            if (
                transfer.FirstRoutePathIndex <=
                fromIndex)
            {
                continue;
            }

            if (
                transfer.SecondRoutePathIndex >=
                toIndex)
            {
                continue;
            }

            double firstRideDistance =
                CalculatePathDistance(
                    firstDirection.Path,
                    fromIndex,
                    transfer.FirstRoutePathIndex);

            double secondRideDistance =
                CalculatePathDistance(
                    secondDirection.Path,
                    transfer.SecondRoutePathIndex,
                    toIndex);

            if (
                firstRideDistance <= 0 ||
                secondRideDistance <= 0)
            {
                continue;
            }

            DirectionEvaluation firstEvaluation =
                CreatePartialEvaluation(
                    firstRoute,
                    firstDirection,
                    from,
                    transfer.Location,
                    fromAccessResult.NearestPoint,
                    transfer.Location,
                    fromIndex,
                    transfer.FirstRoutePathIndex,
                    fromAccessResult.DistanceMeters,
                    transfer.WalkingDistanceMeters,
                    firstRideDistance);

            DirectionEvaluation secondEvaluation =
                CreatePartialEvaluation(
                    secondRoute,
                    secondDirection,
                    transfer.Location,
                    to,
                    transfer.Location,
                    toAccessResult.NearestPoint,
                    transfer.SecondRoutePathIndex,
                    toIndex,
                    transfer.WalkingDistanceMeters,
                    toAccessResult.DistanceMeters,
                    secondRideDistance);

            Journey journey =
                new();

            journey.Legs.Add(
                new JourneyLeg
                {
                    Evaluation =
                        firstEvaluation
                });

            journey.Legs.Add(
                new JourneyLeg
                {
                    Evaluation =
                        secondEvaluation
                });

            journeys.Add(
                journey);
        }
    }

    private DirectionEvaluation CreatePartialEvaluation(
        Route route,
        Direction direction,
        GeoPoint from,
        GeoPoint to,
        GeoPoint nearestFrom,
        GeoPoint nearestTo,
        int fromIndex,
        int toIndex,
        double fromWalkingDistance,
        double toWalkingDistance,
        double rideDistance)
    {
        return new DirectionEvaluation
        {
            Route =
                route,

            DirectionName =
                GetDirectionName(
                    route,
                    direction),

            Direction =
                direction,

            From =
                from,

            To =
                to,

            NearestFrom =
                nearestFrom,

            NearestTo =
                nearestTo,

            FromIndex =
                fromIndex,

            ToIndex =
                toIndex,

            FromWalkingDistance =
                fromWalkingDistance,

            ToWalkingDistance =
                toWalkingDistance,

            TotalWalkingDistance =
                fromWalkingDistance +
                toWalkingDistance,

            RideDistanceMeters =
                rideDistance,

            Viable =
                true
        };
    }

    private IEnumerable<Direction>
        GetDirections(Route route)
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

    private string GetDirectionName(
        Route route,
        Direction direction)
    {
        if (route.Inbound == direction)
        {
            return "Inbound";
        }

        return "Outbound";
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

            if (distance < smallestDistance)
            {
                smallestDistance =
                    distance;

                nearestIndex =
                    i;
            }
        }

        return nearestIndex;
    }

    private double[] BuildCumulativeDistances(
    IList<GeoPoint> path)
    {
        double[] distances =
            new double[path.Count];

        for (int i = 1; i < path.Count; i++)
        {
            distances[i] =
                distances[i - 1] +
                CalculateDistanceMeters(
                    path[i - 1].Latitude,
                    path[i - 1].Longitude,
                    path[i].Latitude,
                    path[i].Longitude);
        }

        return distances;
    }

    private double CalculatePathDistance(
        IList<GeoPoint> path,
        int fromIndex,
        int toIndex)
    {
        if (
            fromIndex < 0 ||
            toIndex >= path.Count ||
            toIndex <= fromIndex)
        {
            return 0;
        }

        double totalDistance = 0;

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

    private RouteBounds GetRouteBounds(
    Direction direction)
    {
        if (
            routeBoundsCache.TryGetValue(
                direction,
                out RouteBounds cachedBounds))
        {
            return cachedBounds;
        }

        IList<GeoPoint> path =
            direction.Path;

        if (path.Count == 0)
        {
            RouteBounds emptyBounds =
                new(
                    0,
                    0,
                    0,
                    0);

            routeBoundsCache[direction] =
                emptyBounds;

            return emptyBounds;
        }

        double minLatitude =
            path[0].Latitude;

        double maxLatitude =
            path[0].Latitude;

        double minLongitude =
            path[0].Longitude;

        double maxLongitude =
            path[0].Longitude;

        for (
            int i = 1;
            i < path.Count;
            i++)
        {
            GeoPoint point =
                path[i];

            minLatitude =
                Math.Min(
                    minLatitude,
                    point.Latitude);

            maxLatitude =
                Math.Max(
                    maxLatitude,
                    point.Latitude);

            minLongitude =
                Math.Min(
                    minLongitude,
                    point.Longitude);

            maxLongitude =
                Math.Max(
                    maxLongitude,
                    point.Longitude);
        }

        RouteBounds bounds =
            new(
                minLatitude,
                maxLatitude,
                minLongitude,
                maxLongitude);

        routeBoundsCache[direction] =
            bounds;

        return bounds;
    }

    private bool CouldRoutesTransfer(
    Direction firstDirection,
    Direction secondDirection,
    double maximumWalkingDistanceMeters)
    {
        if (
            firstDirection.Path.Count == 0 ||
            secondDirection.Path.Count == 0)
        {
            return false;
        }

        RouteBounds firstBounds =
            GetRouteBounds(
                firstDirection);

        RouteBounds secondBounds =
            GetRouteBounds(
                secondDirection);

        double latitudeGapMeters = 0;

        if (
            firstBounds.MaxLatitude <
            secondBounds.MinLatitude)
        {
            latitudeGapMeters =
                CalculateDistanceMeters(
                    firstBounds.MaxLatitude,
                    0,
                    secondBounds.MinLatitude,
                    0);
        }
        else if (
            secondBounds.MaxLatitude <
            firstBounds.MinLatitude)
        {
            latitudeGapMeters =
                CalculateDistanceMeters(
                    secondBounds.MaxLatitude,
                    0,
                    firstBounds.MinLatitude,
                    0);
        }

        double longitudeGapMeters = 0;

        if (
            firstBounds.MaxLongitude <
            secondBounds.MinLongitude)
        {
            longitudeGapMeters =
                CalculateDistanceMeters(
                    0,
                    firstBounds.MaxLongitude,
                    0,
                    secondBounds.MinLongitude);
        }
        else if (
            secondBounds.MaxLongitude <
            firstBounds.MinLongitude)
        {
            longitudeGapMeters =
                CalculateDistanceMeters(
                    0,
                    secondBounds.MaxLongitude,
                    0,
                    firstBounds.MinLongitude);
        }

        double minimumPossibleDistanceMeters =
            Math.Sqrt(
                latitudeGapMeters * latitudeGapMeters +
                longitudeGapMeters * longitudeGapMeters);

        return
            minimumPossibleDistanceMeters <=
            maximumWalkingDistanceMeters;
    }

    private double CalculateDistanceMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadius =
            6371000.0;

        double dLat =
            DegreesToRadians(
                lat2 - lat1);

        double dLon =
            DegreesToRadians(
                lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) *
            Math.Sin(dLat / 2) +

            Math.Cos(
                DegreesToRadians(lat1)) *

            Math.Cos(
                DegreesToRadians(lat2)) *

            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180.0;
    }

    private List<Journey>
        RemoveDuplicateJourneys(
            List<Journey> journeys)
    {
        List<Journey> result =
            new();

        foreach (
            Journey journey
            in journeys)
        {
            bool duplicate =
                result.Any(
                    existing =>
                        AreJourneysEquivalent(
                            existing,
                            journey));

            if (!duplicate)
            {
                result.Add(
                    journey);
            }
        }

        return result;
    }

    private bool AreJourneysEquivalent(
        Journey first,
        Journey second)
    {
        if (
            first.Legs.Count !=
            second.Legs.Count)
        {
            return false;
        }

        for (
            int i = 0;
            i < first.Legs.Count;
            i++)
        {
            if (
                first.Legs[i].RouteId !=
                second.Legs[i].RouteId)
            {
                return false;
            }

            if (
                first.Legs[i].DirectionName !=
                second.Legs[i].DirectionName)
            {
                return false;
            }
        }

        return true;
    }
}