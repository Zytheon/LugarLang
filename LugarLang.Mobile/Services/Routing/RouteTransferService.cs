using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.Mapping;
using LugarLang.Mobile.Services.RoutingSpatial;

namespace LugarLang.Mobile.Services.Routing;

public class RouteTransferService
{
    private readonly RouteAccessibilityService
        routeAccessibilityService;

    private readonly RouteGeometryIndex
        geometryIndex;
    private readonly Dictionary<Direction, double[]>
    cumulativeDistanceCache =
        new();

    public RouteTransferService(
        RouteAccessibilityService routeAccessibilityService,
        RouteGeometryIndex geometryIndex)
    {
        this.routeAccessibilityService =
            routeAccessibilityService;

        this.geometryIndex =
            geometryIndex;
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

        double[] firstCumulativeDistances =
            GetCumulativeDistances(
                firstDirection);

        double[] secondCumulativeDistances =
            GetCumulativeDistances(
                secondDirection);

        List<int> sampledIndices =
            BuildSampledPathIndices(
                firstDirection.Path,
                25.0);

        foreach (
            int firstIndex
            in sampledIndices)
        {
            GeoPoint firstPoint =
                firstDirection.Path[firstIndex];

            RouteAccessResult nearestSecond =
                routeAccessibilityService.FindNearestPointIndexed(
                    secondDirection,
                    firstPoint.Latitude,
                    firstPoint.Longitude,
                    maximumTransferWalkingDistanceMeters);

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
                nearestSecond.SegmentIndex;

            if (
                secondIndex < 0 ||
                secondIndex >= secondDirection.Path.Count)
            {
                continue;
            }

            double firstDistance =
                firstCumulativeDistances[
                    firstIndex];

            double secondDistance =
                secondCumulativeDistances[
                    secondIndex];

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
        const double duplicateDistanceMeters =
            25.0;

        const double cellSizeMeters =
            25.0;

        Dictionary<(int X, int Y), List<TransferPoint>>
            grid =
                new();

        List<TransferPoint> result =
            new();

        foreach (
            TransferPoint transfer
            in transfers)
        {
            double latitude =
                transfer.Location.Latitude;

            double longitude =
                transfer.Location.Longitude;

            double latitudeMeters =
                latitude *
                111320.0;

            double longitudeMeters =
                longitude *
                111320.0 *
                Math.Cos(
                    DegreesToRadians(
                        latitude));

            int cellX =
                (int)Math.Floor(
                    longitudeMeters /
                    cellSizeMeters);

            int cellY =
                (int)Math.Floor(
                    latitudeMeters /
                    cellSizeMeters);

            bool duplicate = false;

            for (
                int offsetX = -1;
                offsetX <= 1;
                offsetX++)
            {
                for (
                    int offsetY = -1;
                    offsetY <= 1;
                    offsetY++)
                {
                    int neighborX =
                        cellX + offsetX;

                    int neighborY =
                        cellY + offsetY;

                    if (
                        !grid.TryGetValue(
                            (neighborX, neighborY),
                            out List<TransferPoint>? nearbyTransfers))
                    {
                        continue;
                    }

                    foreach (
                        TransferPoint existing
                        in nearbyTransfers)
                    {
                        double distance =
                            CalculateDistanceMeters(
                                existing.Location.Latitude,
                                existing.Location.Longitude,
                                latitude,
                                longitude);

                        if (
                            distance <
                            duplicateDistanceMeters)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (duplicate)
                    {
                        break;
                    }
                }

                if (duplicate)
                {
                    break;
                }
            }

            if (duplicate)
            {
                continue;
            }

            result.Add(
                transfer);

            if (
                !grid.TryGetValue(
                    (cellX, cellY),
                    out List<TransferPoint>? cell))
            {
                cell =
                    new List<TransferPoint>();

                grid[
                    (cellX, cellY)] =
                    cell;
            }

            cell.Add(
                transfer);
        }

        return result;
    }

    private List<int> BuildSampledPathIndices(
     IList<GeoPoint> path,
     double sampleSpacingMeters)
    {
        List<int> indices =
            new();

        if (path.Count == 0)
        {
            return indices;
        }

        indices.Add(0);

        double distanceSinceLastSample = 0;

        for (
            int i = 1;
            i < path.Count;
            i++)
        {
            distanceSinceLastSample +=
                CalculateDistanceMeters(
                    path[i - 1].Latitude,
                    path[i - 1].Longitude,
                    path[i].Latitude,
                    path[i].Longitude);

            if (
                distanceSinceLastSample >=
                sampleSpacingMeters)
            {
                indices.Add(i);

                distanceSinceLastSample = 0;
            }
        }

        if (
            indices[indices.Count - 1] !=
            path.Count - 1)
        {
            indices.Add(
                path.Count - 1);
        }

        return indices;
    }

    private double[] GetCumulativeDistances(
    Direction direction)
    {
        if (
            cumulativeDistanceCache.TryGetValue(
                direction,
                out double[]? cachedDistances))
        {
            return cachedDistances;
        }

        double[] distances =
            BuildCumulativeDistances(
                direction.Path);

        cumulativeDistanceCache[
            direction] =
            distances;

        return distances;
    }

    private double[] BuildCumulativeDistances(
    IList<GeoPoint> path)
    {
        double[] distances =
            new double[path.Count];

        for (
            int i = 1;
            i < path.Count;
            i++)
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

