using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.RoutingSpatial;

/// <summary>
/// One resolved graph node: a precomputed (routeId, direction, pathIndex)
/// triple resolved against the live TransportNetwork, so its coordinate
/// always matches Route.Direction.Path -- no coordinate data is duplicated
/// or able to drift out of sync with routes.json.
/// </summary>
public class RouteGraphResolvedNode
{
    public Route Route { get; init; } = null!;

    public string DirectionName { get; init; } = "";

    public Direction Direction { get; init; } = null!;

    public int PathIndex { get; init; }

    public GeoPoint Point { get; init; } = null!;
}

public class RouteGraphEdge
{
    public int ToNode { get; init; }

    public double DistanceMeters { get; init; }

    public bool IsTransfer { get; init; }
}

/// <summary>
/// In-memory representation of the precomputed segment/hub graph. Built
/// once at service construction from the bundled routegraph.json asset --
/// no per-query graph building. Ride edges are directional (can only ride
/// forward along a direction's path); transfer edges are bidirectional
/// walking connections and are filtered by the caller's chosen maximum
/// walking distance at query time (they're all precomputed up to the
/// asset's MaxTransferDistanceMeters, the largest walkingDistancePicker
/// option).
/// </summary>
public class RouteGraph
{
    private const double CellSizeDegrees = 0.005;

    private readonly RouteGraphResolvedNode[] nodes;
    private readonly List<RouteGraphEdge>[] adjacency;
    private readonly Dictionary<(int, int), List<int>> spatialGrid = new();

    public double MaxTransferDistanceMeters { get; }

    public int NodeCount => nodes.Length;

    public RouteGraph(
        RouteGraphAsset asset,
        IEnumerable<Route> routes)
    {
        MaxTransferDistanceMeters =
            asset.MaxTransferDistanceMeters;

        Dictionary<(string, string), (Route Route, Direction Direction)> directionLookup =
            new();

        foreach (Route route in routes)
        {
            if (route.Inbound != null)
            {
                directionLookup[(route.Id, "inbound")] =
                    (route, route.Inbound);
            }

            if (route.Outbound != null)
            {
                directionLookup[(route.Id, "outbound")] =
                    (route, route.Outbound);
            }
        }

        nodes = new RouteGraphResolvedNode[asset.Nodes.Count];

        for (int i = 0; i < asset.Nodes.Count; i++)
        {
            RouteGraphNodeAsset raw = asset.Nodes[i];

            if (!directionLookup.TryGetValue(
                    (raw.RouteId, raw.Direction),
                    out (Route Route, Direction Direction) resolved))
            {
                throw new InvalidOperationException(
                    $"routegraph.json references route '{raw.RouteId}' " +
                    $"direction '{raw.Direction}', which was not found in " +
                    "the loaded TransportNetwork. The graph asset and " +
                    "routes.json have gone out of sync -- rebuild the " +
                    "asset via build_graph.py.");
            }

            if (raw.PathIndex < 0 ||
                raw.PathIndex >= resolved.Direction.Path.Count)
            {
                throw new InvalidOperationException(
                    $"routegraph.json node references pathIndex " +
                    $"{raw.PathIndex} on '{raw.RouteId}' " +
                    $"'{raw.Direction}', but that direction only has " +
                    $"{resolved.Direction.Path.Count} path points.");
            }

            nodes[i] = new RouteGraphResolvedNode
            {
                Route = resolved.Route,
                DirectionName =
                    raw.Direction == "inbound" ? "Inbound" : "Outbound",
                Direction = resolved.Direction,
                PathIndex = raw.PathIndex,
                Point = resolved.Direction.Path[raw.PathIndex]
            };
        }

        adjacency = new List<RouteGraphEdge>[nodes.Length];
        for (int i = 0; i < adjacency.Length; i++)
        {
            adjacency[i] = new List<RouteGraphEdge>();
        }

        foreach (double[] edge in asset.RideEdges)
        {
            int from = (int)edge[0];
            int to = (int)edge[1];
            double distance = edge[2];

            // Ride edges are directional: you can only travel forward
            // along the path the way a passenger actually rides it.
            adjacency[from].Add(
                new RouteGraphEdge
                {
                    ToNode = to,
                    DistanceMeters = distance,
                    IsTransfer = false
                });
        }

        foreach (double[] edge in asset.TransferEdges)
        {
            int a = (int)edge[0];
            int b = (int)edge[1];
            double distance = edge[2];

            // Transfer edges are walking connections: bidirectional.
            adjacency[a].Add(
                new RouteGraphEdge
                {
                    ToNode = b,
                    DistanceMeters = distance,
                    IsTransfer = true
                });

            adjacency[b].Add(
                new RouteGraphEdge
                {
                    ToNode = a,
                    DistanceMeters = distance,
                    IsTransfer = true
                });
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            (int cellLatitude, int cellLongitude) =
                GetCell(
                    nodes[i].Point.Latitude,
                    nodes[i].Point.Longitude);

            var cell = (cellLatitude, cellLongitude);

            if (!spatialGrid.TryGetValue(
                    cell,
                    out List<int>? cellNodes))
            {
                cellNodes = new List<int>();
                spatialGrid[cell] = cellNodes;
            }

            cellNodes.Add(i);
        }
    }

    public RouteGraphResolvedNode GetNode(int nodeId)
    {
        return nodes[nodeId];
    }

    public IReadOnlyList<RouteGraphEdge> GetEdges(int nodeId)
    {
        return adjacency[nodeId];
    }

    /// <summary>
    /// Diagnostic only. Logs every distinct (route, direction) present in
    /// the graph, with its node count. Call this once after construction
    /// when investigating "why doesn't route X show up" -- if a route
    /// isn't in this list, it never made it into routegraph.json at all
    /// (a build_graph.py / asset problem, e.g. the route had fewer than
    /// 2 path points and got skipped), which is a different problem than
    /// it being present but not found near a particular pin.
    /// </summary>
    public void LogRouteSummary()
    {
        var distinctDirections =
            nodes
                .Select(n => (n.Route.Id, n.Route.Name, n.DirectionName))
                .Distinct()
                .GroupBy(x => (x.Id, x.Name, x.DirectionName))
                .Select(g => (g.Key.Id, g.Key.Name, g.Key.DirectionName,
                    NodeCount: nodes.Count(n =>
                        n.Route.Id == g.Key.Id &&
                        n.DirectionName == g.Key.DirectionName)))
                .OrderBy(x => x.Id)
                .ThenBy(x => x.DirectionName)
                .ToList();

        System.Diagnostics.Debug.WriteLine(
            $"[ROUTE GRAPH] {distinctDirections.Count} route-directions " +
            $"present, {nodes.Length} total nodes:");

        foreach (var (id, name, directionName, nodeCount) in distinctDirections)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ROUTE GRAPH]   {id} ({name}) {directionName} -- " +
                $"{nodeCount} nodes");
        }
    }

    /// <summary>
    /// Finds, for each distinct (route, direction) with at least one node
    /// within maximumDistanceMeters of the given point, the single nearest
    /// such node. Mirrors the existing engine's "one entry state per
    /// route+direction" behavior (TransitRoutingEngine.FindInitialStates)
    /// -- multiple routes near the same point each become a separate
    /// candidate journey start/end, letting the search pick the best
    /// combination rather than committing to one route upfront.
    ///
    /// Note: this snaps to the nearest precomputed NODE (sampled every
    /// ~25m), not the nearest point on the path segment the way
    /// RouteAccessibilityService does. This trades a small amount of
    /// precision (up to roughly half the sample spacing) for not needing
    /// any per-query geometry work beyond the bundled node set.
    /// </summary>
    public List<(int NodeId, double DistanceMeters)>
        FindNearestNodePerDirection(
            double latitude,
            double longitude,
            double maximumDistanceMeters)
    {
        double latitudeRadius =
            maximumDistanceMeters / 111320.0;

        double longitudeRadius =
            maximumDistanceMeters /
            (111320.0 * Math.Cos(latitude * Math.PI / 180.0));

        int cellLatitudeSpan =
            (int)Math.Ceiling(latitudeRadius / CellSizeDegrees) + 1;

        int cellLongitudeSpan =
            (int)Math.Ceiling(longitudeRadius / CellSizeDegrees) + 1;

        (int baseLatitude, int baseLongitude) =
            GetCell(latitude, longitude);

        Dictionary<(Route, string), (double Distance, int NodeId)>
            bestByDirection = new();

        for (int dLat = -cellLatitudeSpan; dLat <= cellLatitudeSpan; dLat++)
        {
            for (int dLon = -cellLongitudeSpan; dLon <= cellLongitudeSpan; dLon++)
            {
                var cell = (baseLatitude + dLat, baseLongitude + dLon);

                if (!spatialGrid.TryGetValue(
                        cell,
                        out List<int>? cellNodes))
                {
                    continue;
                }

                foreach (int nodeId in cellNodes)
                {
                    RouteGraphResolvedNode node = nodes[nodeId];

                    double distance =
                        CalculateDistanceMeters(
                            latitude,
                            longitude,
                            node.Point.Latitude,
                            node.Point.Longitude);

                    if (distance > maximumDistanceMeters)
                    {
                        continue;
                    }

                    var key = (node.Route, node.DirectionName);

                    if (!bestByDirection.TryGetValue(
                            key,
                            out (double Distance, int NodeId) current) ||
                        distance < current.Distance)
                    {
                        bestByDirection[key] = (distance, nodeId);
                    }
                }
            }
        }

        return bestByDirection.Values
            .Select(v => (v.NodeId, v.Distance))
            .ToList();
    }

    private (int, int) GetCell(double latitude, double longitude)
    {
        return (
            (int)Math.Floor(latitude / CellSizeDegrees),
            (int)Math.Floor(longitude / CellSizeDegrees));
    }

    private double CalculateDistanceMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadius = 6371000.0;

        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) *
            Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadius * c;
    }
}