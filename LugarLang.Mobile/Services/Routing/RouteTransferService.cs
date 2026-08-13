using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;

namespace LugarLang.Mobile.Services.Routing;

public class RouteTransferService
{
    private readonly RouteAccessibilityService
        routeAccessibilityService;

    public RouteTransferService(
        RouteAccessibilityService routeAccessibilityService)
    {
        this.routeAccessibilityService =
            routeAccessibilityService;
    }

    public List<TransferPoint> FindTransferPoints(
        Direction firstDirection,
        Direction secondDirection,
        double maximumTransferWalkingDistanceMeters)
    {
        List<TransferPoint> transfers =
            new();

        if (
            firstDirection.Path == null ||
            secondDirection.Path == null ||
            firstDirection.Path.Count == 0 ||
            secondDirection.Path.Count == 0)
        {
            return transfers;
        }

        for (
            int firstIndex = 0;
            firstIndex < firstDirection.Path.Count;
            firstIndex++)
        {
            GeoPoint firstPoint =
                firstDirection.Path[firstIndex];

            RouteAccessResult nearestSecond =
                routeAccessibilityService.FindNearestPoint(
                    firstPoint.Latitude,
                    firstPoint.Longitude,
                    secondDirection.Path);

            if (
                nearestSecond.NearestPoint == null)
            {
                continue;
            }

            if (
                nearestSecond.DistanceMeters >
                maximumTransferWalkingDistanceMeters)
            {
                continue;
            }

            int secondIndex =
                FindNearestPathIndex(
                    nearestSecond.NearestPoint,
                    secondDirection.Path);

            double firstDistance =
                CalculatePathDistance(
                    firstDirection.Path,
                    0,
                    firstIndex);

            double secondDistance =
                CalculatePathDistance(
                    secondDirection.Path,
                    0,
                    secondIndex);

            transfers.Add(
                new TransferPoint
                {
                    Location =
                        firstPoint,

                    FirstRouteDistanceFromStartMeters =
                        firstDistance,

                    SecondRouteDistanceFromStartMeters =
                        secondDistance,

                    WalkingDistanceMeters =
                        nearestSecond.DistanceMeters,

                    FirstRoutePathIndex =
                        firstIndex,

                    SecondRoutePathIndex =
                        secondIndex
                });
        }

        return RemoveDuplicateTransfers(
            transfers);
    }

    private List<TransferPoint>
        RemoveDuplicateTransfers(
            List<TransferPoint> transfers)
    {
        List<TransferPoint> result =
            new();

        foreach (
            TransferPoint transfer
            in transfers)
        {
            bool duplicate =
                result.Any(
                    existing =>
                        CalculateDistanceMeters(
                            existing.Location.Latitude,
                            existing.Location.Longitude,
                            transfer.Location.Latitude,
                            transfer.Location.Longitude)
                        < 25.0);

            if (!duplicate)
            {
                result.Add(
                    transfer);
            }
        }

        return result;
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
            path.Count < 2 ||
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
}
