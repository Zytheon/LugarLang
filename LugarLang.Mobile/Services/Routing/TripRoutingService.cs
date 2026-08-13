using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;

namespace LugarLang.Mobile.Services.Routing;

public class TripRoutingService
{
    private readonly RouteAccessibilityService routeAccessibilityService;

    public TripRoutingService(
        RouteAccessibilityService routeAccessibilityService)
    {
        this.routeAccessibilityService =
            routeAccessibilityService;
    }

    public DirectionEvaluation? SelectBestDirection(
        Route route,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        DirectionEvaluation? inbound =
            EvaluateDirection(
                route,
                "Inbound",
                route.Inbound,
                from,
                to,
                maximumWalkingDistanceMeters);

        DirectionEvaluation? outbound =
            EvaluateDirection(
                route,
                "Outbound",
                route.Outbound,
                from,
                to,
                maximumWalkingDistanceMeters);

        if (inbound != null &&
            inbound.Viable)
        {
            return inbound;
        }

        if (outbound != null &&
            outbound.Viable)
        {
            return outbound;
        }

        return null;
    }

    public List<DirectionEvaluation> EvaluateAllTrips(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        List<DirectionEvaluation> evaluations =
            new();

        foreach (Route route in routes)
        {
            DirectionEvaluation? inbound =
                EvaluateDirection(
                    route,
                    "Inbound",
                    route.Inbound,
                    from,
                    to,
                    maximumWalkingDistanceMeters);

            DirectionEvaluation? outbound =
                EvaluateDirection(
                    route,
                    "Outbound",
                    route.Outbound,
                    from,
                    to,
                    maximumWalkingDistanceMeters);

            if (inbound != null)
            {
                evaluations.Add(inbound);
            }

            if (outbound != null)
            {
                evaluations.Add(outbound);
            }
        }

        return evaluations;
    }

    public DirectionEvaluation? SelectBestTrip(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        DirectionEvaluation? bestTrip =
            null;

        foreach (Route route in routes)
        {
            DirectionEvaluation? inbound =
                EvaluateDirection(
                    route,
                    "Inbound",
                    route.Inbound,
                    from,
                    to,
                    maximumWalkingDistanceMeters);

            DirectionEvaluation? outbound =
                EvaluateDirection(
                    route,
                    "Outbound",
                    route.Outbound,
                    from,
                    to,
                    maximumWalkingDistanceMeters);

            if (inbound != null &&
                inbound.Viable)
            {
                bestTrip =
                    SelectBetterTrip(
                        bestTrip,
                        inbound);
            }

            if (outbound != null &&
                outbound.Viable)
            {
                bestTrip =
                    SelectBetterTrip(
                        bestTrip,
                        outbound);
            }
        }

        return bestTrip;
    }

    private DirectionEvaluation? EvaluateDirection(
        Route route,
        string directionName,
        Direction? direction,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        if (direction == null ||
            direction.Path.Count == 0)
        {
            return null;
        }

        RouteAccessResult fromResult =
            routeAccessibilityService.FindNearestPoint(
                from.Latitude,
                from.Longitude,
                direction.Path);

        RouteAccessResult toResult =
            routeAccessibilityService.FindNearestPoint(
                to.Latitude,
                to.Longitude,
                direction.Path);

        if (fromResult.NearestPoint == null ||
            toResult.NearestPoint == null)
        {
            return null;
        }

        bool fromAccessible =
            fromResult.DistanceMeters <=
            maximumWalkingDistanceMeters;

        bool toAccessible =
            toResult.DistanceMeters <=
            maximumWalkingDistanceMeters;

        int fromIndex =
            FindNearestPathIndex(
                fromResult.NearestPoint,
                direction.Path);

        int toIndex =
            FindNearestPathIndex(
                toResult.NearestPoint,
                direction.Path);

        bool directionCorrect =
            toIndex > fromIndex;

        bool viable =
            fromAccessible &&
            toAccessible &&
            directionCorrect;

        double rideDistance =
            CalculatePathDistance(
                direction.Path,
                fromIndex,
                toIndex);

        return new DirectionEvaluation
        {
            Route =
                route,

            DirectionName =
                directionName,

            Direction =
                direction,

            From =
                from,

            To =
                to,

            NearestFrom =
                fromResult.NearestPoint,

            NearestTo =
                toResult.NearestPoint,

            FromIndex =
                fromIndex,

            ToIndex =
                toIndex,

            FromWalkingDistance =
                fromResult.DistanceMeters,

            ToWalkingDistance =
                toResult.DistanceMeters,

            TotalWalkingDistance =
                fromResult.DistanceMeters +
                toResult.DistanceMeters,

            RideDistanceMeters =
                rideDistance,

            Viable =
                viable
        };
    }

    private DirectionEvaluation SelectBetterTrip(
        DirectionEvaluation? current,
        DirectionEvaluation candidate)
    {
        if (current == null)
        {
            return candidate;
        }

        if (candidate.TotalWalkingDistance <
            current.TotalWalkingDistance)
        {
            return candidate;
        }

        return current;
    }

    private double CalculatePathDistance(
        IList<GeoPoint> path,
        int fromIndex,
        int toIndex)
    {
        if (fromIndex < 0 ||
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

            if (distance <
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
}
