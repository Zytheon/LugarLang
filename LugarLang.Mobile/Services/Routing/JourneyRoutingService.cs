using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;

namespace LugarLang.Mobile.Services.Routing;

public class JourneyRoutingService
{
    private readonly TripRoutingService tripRoutingService;

    private readonly RouteTransferService routeTransferService;

    public JourneyRoutingService(
        TripRoutingService tripRoutingService,
        RouteTransferService routeTransferService)
    {
        this.tripRoutingService =
            tripRoutingService;

        this.routeTransferService =
            routeTransferService;
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
        foreach (
            Route firstRoute
            in routes)
        {
            foreach (
                Route secondRoute
                in routes)
            {
                foreach (
                    Direction firstDirection
                    in GetDirections(firstRoute))
                {
                    foreach (
                        Direction secondDirection
                        in GetDirections(secondRoute))
                    {
                        if (
                            firstRoute ==
                            secondRoute &&
                            firstDirection ==
                            secondDirection)
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
                            maximumWalkingDistanceMeters);
                    }
                }
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
        double maximumWalkingDistanceMeters)
    {
        RouteAccessResult fromResult =
            FindNearestPoint(
                from,
                firstDirection);

        RouteAccessResult toResult =
            FindNearestPoint(
                to,
                secondDirection);

        if (
            fromResult.NearestPoint == null ||
            toResult.NearestPoint == null)
        {
            return;
        }

        if (
            fromResult.DistanceMeters >
            maximumWalkingDistanceMeters)
        {
            return;
        }

        if (
            toResult.DistanceMeters >
            maximumWalkingDistanceMeters)
        {
            return;
        }

        int fromIndex =
            FindNearestPathIndex(
                fromResult.NearestPoint,
                firstDirection.Path);

        int toIndex =
            FindNearestPathIndex(
                toResult.NearestPoint,
                secondDirection.Path);

        List<TransferPoint> transfers =
            routeTransferService.FindTransferPoints(
                firstDirection,
                secondDirection,
                maximumWalkingDistanceMeters);

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
                    fromResult.NearestPoint,
                    transfer.Location,
                    fromIndex,
                    transfer.FirstRoutePathIndex,
                    fromResult.DistanceMeters,
                    transfer.WalkingDistanceMeters,
                    firstRideDistance);

            DirectionEvaluation secondEvaluation =
                CreatePartialEvaluation(
                    secondRoute,
                    secondDirection,
                    transfer.Location,
                    to,
                    transfer.Location,
                    toResult.NearestPoint,
                    transfer.SecondRoutePathIndex,
                    toIndex,
                    transfer.WalkingDistanceMeters,
                    toResult.DistanceMeters,
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

    private RouteAccessResult FindNearestPoint(
        GeoPoint point,
        Direction direction)
    {
        RouteAccessibilityService accessibility =
            GetAccessibilityService();

        return accessibility.FindNearestPoint(
            point.Latitude,
            point.Longitude,
            direction.Path);
    }

    private RouteAccessibilityService
        GetAccessibilityService()
    {
        return
            (RouteAccessibilityService)
            typeof(TripRoutingService)
                .GetField(
                    "routeAccessibilityService",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)!
                .GetValue(
                    tripRoutingService)!;
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
        if (
            route.Inbound ==
            direction)
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
