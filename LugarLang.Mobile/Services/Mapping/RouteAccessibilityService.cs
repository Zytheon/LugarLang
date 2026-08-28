using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.RoutingSpatial;

namespace LugarLang.Mobile.Services.Mapping;

public class RouteAccessibilityService
{
    private const double EarthRadiusMeters = 6371000.0;

    private readonly RouteGeometryIndex geometryIndex;

    public RouteAccessibilityService(
        RouteGeometryIndex geometryIndex)
    {
        this.geometryIndex =
            geometryIndex;
    }

    public RouteAccessResult FindNearestPoint(
        double latitude,
        double longitude,
        IEnumerable<GeoPoint> path)
    {
        List<GeoPoint> points =
            path.ToList();

        if (points.Count == 0)
        {
            return new RouteAccessResult
            {
                DistanceMeters =
                    double.MaxValue,

                NearestPoint =
                    null
            };
        }

        if (points.Count == 1)
        {
            return new RouteAccessResult
            {
                DistanceMeters =
                    CalculateDistanceMeters(
                        latitude,
                        longitude,
                        points[0].Latitude,
                        points[0].Longitude),

                NearestPoint =
                    new GeoPoint
                    {
                        Latitude =
                            points[0].Latitude,

                        Longitude =
                            points[0].Longitude
                    }
            };
        }

        double bestDistance =
            double.MaxValue;

        GeoPoint? bestPoint =
            null;


        for (
            int i = 0;
            i < points.Count - 1;
            i++)
        {
            GeoPoint a =
                points[i];

            GeoPoint b =
                points[i + 1];

            GeoPoint candidate =
                FindNearestPointOnSegment(
                    latitude,
                    longitude,
                    a,
                    b);

            double distance =
                CalculateDistanceMeters(
                    latitude,
                    longitude,
                    candidate.Latitude,
                    candidate.Longitude);

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestPoint =
                    candidate;

  
            }
        }

        return new RouteAccessResult
        {
            DistanceMeters =
                bestDistance,

            NearestPoint =
                bestPoint,


        };
    }

    public RouteAccessResult FindNearestPointIndexed(
        double latitude,
        double longitude,
        double searchRadiusMeters)
    {
        IReadOnlyList<IndexedSegment> nearbySegments =
            geometryIndex.FindNearbySegments(
                latitude,
                longitude,
                searchRadiusMeters);

        if (nearbySegments.Count == 0)
        {
            return new RouteAccessResult
            {
                DistanceMeters =
                    double.MaxValue,

                NearestPoint =
                    null
            };
        }

        double bestDistance =
            double.MaxValue;

        GeoPoint? bestPoint =
            null;

        int bestSegmentIndex =
            -1;

        foreach (
            IndexedSegment segment
            in nearbySegments)
        {
            GeoPoint candidate =
                FindNearestPointOnSegment(
                    latitude,
                    longitude,
                    segment.Start,
                    segment.End);

            double distance =
                CalculateDistanceMeters(
                    latitude,
                    longitude,
                    candidate.Latitude,
                    candidate.Longitude);

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestPoint =
                    candidate;

                bestSegmentIndex =
                    segment.SegmentIndex;
            }
        }

        return new RouteAccessResult
        {
            DistanceMeters =
                bestDistance,

            NearestPoint =
                bestPoint,

            SegmentIndex =
                bestSegmentIndex
        };
    }

    public RouteAccessResult FindNearestPointIndexed(
    Direction direction,
    double latitude,
    double longitude,
    double searchRadiusMeters)
    {
        IReadOnlyList<IndexedSegment> nearbySegments =
            geometryIndex.FindNearbySegments(
                direction,
                latitude,
                longitude,
                searchRadiusMeters);

        if (nearbySegments.Count == 0)
        {
            return new RouteAccessResult
            {
                DistanceMeters =
                    double.MaxValue,

                NearestPoint =
                    null
            };
        }

        double bestDistance =
            double.MaxValue;

        GeoPoint? bestPoint =
            null;

        foreach (
            IndexedSegment segment
            in nearbySegments)
        {
            GeoPoint candidate =
                FindNearestPointOnSegment(
                    latitude,
                    longitude,
                    segment.Start,
                    segment.End);

            double distance =
                CalculateDistanceMeters(
                    latitude,
                    longitude,
                    candidate.Latitude,
                    candidate.Longitude);

            if (distance < bestDistance)
            {
                bestDistance =
                    distance;

                bestPoint =
                    candidate;
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

    private GeoPoint FindNearestPointOnSegment(
        double latitude,
        double longitude,
        GeoPoint a,
        GeoPoint b)
    {
        double referenceLatitude =
            latitude *
            Math.PI /
            180.0;

        double scale =
            Math.Cos(
                referenceLatitude);

        double ax =
            a.Longitude *
            scale;

        double ay =
            a.Latitude;

        double bx =
            b.Longitude *
            scale;

        double by =
            b.Latitude;

        double px =
            longitude *
            scale;

        double py =
            latitude;

        double dx =
            bx -
            ax;

        double dy =
            by -
            ay;

        double lengthSquared =
            dx * dx +
            dy * dy;

        if (lengthSquared == 0)
        {
            return new GeoPoint
            {
                Latitude =
                    a.Latitude,

                Longitude =
                    a.Longitude
            };
        }

        double t =
            ((px - ax) * dx +
             (py - ay) * dy) /
            lengthSquared;

        t =
            Math.Max(
                0,
                Math.Min(
                    1,
                    t));

        double nearestX =
            ax +
            t * dx;

        double nearestY =
            ay +
            t * dy;

        return new GeoPoint
        {
            Latitude =
                nearestY,

            Longitude =
                nearestX / scale
        };
    }

    private double CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
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
            Math.Sin(
                deltaLat / 2) *
            Math.Sin(
                deltaLat / 2) +

            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Sin(
                deltaLon / 2) *
            Math.Sin(
                deltaLon / 2);

        double c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return
            EarthRadiusMeters *
            c;
    }
}

public class RouteAccessResult
{
    public double DistanceMeters { get; set; }

    public GeoPoint? NearestPoint { get; set; }

    public int SegmentIndex { get; set; } = -1;
}