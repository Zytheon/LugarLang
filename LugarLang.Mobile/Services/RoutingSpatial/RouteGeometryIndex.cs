using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.RoutingSpatial;

public class RouteGeometryIndex
{
    private const double CellSizeDegrees = 0.002;
    private readonly List<IndexedSegment> segments =
        new();

    private readonly Dictionary<
        (int Latitude,
         int Longitude),
        List<IndexedSegment>> spatialGrid =
        new();

    private (int Latitude, int Longitude) GetCell(
    double latitude,
    double longitude)
    {
        return (
            (int)Math.Floor(
                latitude / CellSizeDegrees),

            (int)Math.Floor(
                longitude / CellSizeDegrees));
    }

    public IReadOnlyList<IndexedSegment> FindNearbySegments(
    double latitude,
    double longitude,
    double searchRadiusMeters)
    {
        double latitudeRadius =
            searchRadiusMeters / 111320.0;

        double longitudeRadius =
            searchRadiusMeters /
            (
                111320.0 *
                Math.Cos(
                    latitude *
                    Math.PI /
                    180.0)
            );

        double minLatitude =
            latitude -
            latitudeRadius;

        double maxLatitude =
            latitude +
            latitudeRadius;

        double minLongitude =
            longitude -
            longitudeRadius;

        double maxLongitude =
            longitude +
            longitudeRadius;

        (int minLatitudeCell, int minLongitudeCell) =
            GetCell(
                minLatitude,
                minLongitude);

        (int maxLatitudeCell, int maxLongitudeCell) =
            GetCell(
                maxLatitude,
                maxLongitude);

        HashSet<IndexedSegment> candidates =
            new();

        for (
            int latitudeCell = minLatitudeCell;
            latitudeCell <= maxLatitudeCell;
            latitudeCell++)
        {
            for (
                int longitudeCell = minLongitudeCell;
                longitudeCell <= maxLongitudeCell;
                longitudeCell++)
            {
                var cell =
                    (latitudeCell, longitudeCell);

                if (!spatialGrid.TryGetValue(
                        cell,
                        out List<IndexedSegment>? cellSegments))
                {
                    continue;
                }

                foreach (
                    IndexedSegment segment
                    in cellSegments)
                {
                    candidates.Add(
                        segment);
                }
            }
        }

        return candidates
            .Where(
                segment =>
                    SegmentIntersectsBounds(
                        segment,
                        minLatitude,
                        maxLatitude,
                        minLongitude,
                        maxLongitude))
            .ToList();
    }

    public IReadOnlyList<IndexedSegment> FindNearbySegments(
    Direction direction,
    double latitude,
    double longitude,
    double searchRadiusMeters)
    {
        IReadOnlyList<IndexedSegment> nearbySegments =
            FindNearbySegments(
                latitude,
                longitude,
                searchRadiusMeters);

        return nearbySegments
            .Where(
                segment =>
                    ReferenceEquals(
                        segment.Direction,
                        direction))
            .ToList();
    }

    private bool SegmentIntersectsBounds(
    IndexedSegment segment,
    double minLatitude,
    double maxLatitude,
    double minLongitude,
    double maxLongitude)
    {
        double segmentMinLatitude =
            Math.Min(
                segment.Start.Latitude,
                segment.End.Latitude);

        double segmentMaxLatitude =
            Math.Max(
                segment.Start.Latitude,
                segment.End.Latitude);

        double segmentMinLongitude =
            Math.Min(
                segment.Start.Longitude,
                segment.End.Longitude);

        double segmentMaxLongitude =
            Math.Max(
                segment.Start.Longitude,
                segment.End.Longitude);

        return
            segmentMaxLatitude >= minLatitude &&
            segmentMinLatitude <= maxLatitude &&
            segmentMaxLongitude >= minLongitude &&
            segmentMinLongitude <= maxLongitude;
    }


    public RouteGeometryIndex(
        IEnumerable<Route> routes)
    {
        foreach (Route route in routes)
        {
            AddDirection(
                route,
                route.Inbound,
                "Inbound");

            AddDirection(
                route,
                route.Outbound,
                "Outbound");
        }


    }

    private void AddDirection(
        Route route,
        Direction? direction,
        string directionName)
    {
        if (direction == null)
        {
            return;
        }

        if (direction.Path.Count < 2)
        {
            return;
        }

        for (
            int i = 0;
            i < direction.Path.Count - 1;
            i++)
        {
            GeoPoint a =
                direction.Path[i];

            GeoPoint b =
                direction.Path[i + 1];

            IndexedSegment segment =
    new()
    {
        Route = route,
        Direction = direction,
        DirectionName = directionName,
        SegmentIndex = i,
        Start = a,
        End = b
    };

            segments.Add(
                segment);

            (int startLatitude, int startLongitude) =
                GetCell(
                    a.Latitude,
                    a.Longitude);

            (int endLatitude, int endLongitude) =
                GetCell(
                    b.Latitude,
                    b.Longitude);

            int minLatitude =
                Math.Min(
                    startLatitude,
                    endLatitude);

            int maxLatitude =
                Math.Max(
                    startLatitude,
                    endLatitude);

            int minLongitude =
                Math.Min(
                    startLongitude,
                    endLongitude);

            int maxLongitude =
                Math.Max(
                    startLongitude,
                    endLongitude);

            for (
                int latitudeCell = minLatitude;
                latitudeCell <= maxLatitude;
                latitudeCell++)
            {
                for (
                    int longitudeCell = minLongitude;
                    longitudeCell <= maxLongitude;
                    longitudeCell++)
                {
                    var cell =
                        (latitudeCell, longitudeCell);

                    if (!spatialGrid.TryGetValue(
                            cell,
                            out List<IndexedSegment>? cellSegments))
                    {
                        cellSegments =
                            new List<IndexedSegment>();

                        spatialGrid[cell] =
                            cellSegments;
                    }

                    cellSegments.Add(
                        segment);
                }
            }
        }
    }

    public IReadOnlyList<IndexedSegment> Segments =>
        segments;
}

public class IndexedSegment
{
    public Route Route { get; init; } = null!;

    public Direction Direction { get; init; } = null!;

    public string DirectionName { get; init; } = "";

    public int SegmentIndex { get; init; }

    public GeoPoint Start { get; init; } = null!;

    public GeoPoint End { get; init; } = null!;
}