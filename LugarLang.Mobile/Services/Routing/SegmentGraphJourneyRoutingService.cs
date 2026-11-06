using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services.RoutingSpatial;

namespace LugarLang.Mobile.Services.Routing;

/// <summary>
/// FindJourneys implementation that runs Dijkstra's algorithm over a graph
/// that was fully precomputed offline and ships bundled in the APK as
/// routegraph.json. No graph-building work happens on-device or at query
/// time -- only nearest-node snapping and the search itself.
///
/// The search cost is an estimate of travel TIME in seconds (walking and
/// riding at different speeds, plus a penalty per transfer), not raw
/// meters. After the search, impractical journeys are filtered out: too
/// many rides, tiny rides inside a multi-ride trip, and multi-ride trips
/// that do not clearly beat a simpler option.
/// </summary>
public class SegmentGraphJourneyRoutingService : IJourneyRoutingService
{
    private readonly RouteGraph graph;

    /// <summary>
    /// How many (start route, end route) pairs get their own Dijkstra
    /// search, taken in order of combined walking distance (closest
    /// pairs first). Same-route pairs are always searched on top of this.
    /// </summary>
    private const int MaximumCandidatePairsSearched = 20;

    /// <summary>
    /// Assumed walking speed, about 5 km/h. Used to turn walking meters
    /// into seconds, both in the search cost and in time estimates.
    /// </summary>
    private const double WalkSpeedMetersPerSecond = 1.4;

    /// <summary>
    /// Assumed average speed while riding, about 15 km/h including stops
    /// and traffic. A guess -- tune it against real trips.
    /// </summary>
    private const double RideSpeedMetersPerSecond = 4.2;

    /// <summary>
    /// Flat time cost added to every transfer edge, standing in for the
    /// wait for the next vehicle, flagging it down, and the extra fare
    /// and hassle. Only applied to edges between two different routes,
    /// never to the first or last walk.
    /// </summary>
    private const double TransferPenaltySeconds = 480.0;

    /// <summary>
    /// Journeys with more rides than this are discarded.
    /// </summary>
    private const int MaximumRides = 3;

    /// <summary>
    /// In a journey with more than one ride, every ride must be at least
    /// this long. Shorter rides are usually just hopping between routes
    /// that run along the same street.
    /// </summary>
    private const double MinimumRideMetersInMultiRide = 300.0;

    /// <summary>
    /// A journey is kept only if it is at least this much faster
    /// (walking time + riding time) than the best journey with fewer
    /// rides. Prevents showing an extra transfer for a tiny saving.
    /// </summary>
    private const double MinimumSavingsSeconds = 600.0;

    public SegmentGraphJourneyRoutingService(RouteGraph graph)
    {
        this.graph = graph;
    }

    public List<Journey> FindJourneys(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters)
    {
        // 'routes' is accepted to match the IJourneyRoutingService
        // signature, but isn't otherwise used here -- the graph was
        // already resolved against the live route set when RouteGraph
        // was built.
        _ = routes;

        System.Diagnostics.Debug.WriteLine(
            $"[NEW ALGORITHM] FindJourneys starting: " +
            $"maxWalk={maximumWalkingDistanceMeters}m, " +
            $"graph has {graph.NodeCount} nodes");

        System.Diagnostics.Stopwatch queryTimer =
            System.Diagnostics.Stopwatch.StartNew();

        System.Diagnostics.Stopwatch snapTimer =
            System.Diagnostics.Stopwatch.StartNew();

        List<(int NodeId, double DistanceMeters)> startCandidates =
            graph.FindNearestNodePerDirection(
                from.Latitude, from.Longitude, maximumWalkingDistanceMeters)
                .OrderBy(c => c.DistanceMeters)
                .ToList();

        List<(int NodeId, double DistanceMeters)> endCandidates =
            graph.FindNearestNodePerDirection(
                to.Latitude, to.Longitude, maximumWalkingDistanceMeters)
                .OrderBy(c => c.DistanceMeters)
                .ToList();

        snapTimer.Stop();

        System.Diagnostics.Debug.WriteLine(
            $"[NEW ALGORITHM]   NEAREST-NODE SNAP: " +
            $"{snapTimer.ElapsedMilliseconds}ms -- " +
            $"{startCandidates.Count} start candidates, " +
            $"{endCandidates.Count} end candidates");

        System.Diagnostics.Debug.WriteLine(
            "[NEW ALGORITHM]   START CANDIDATES (near From):");

        foreach ((int nodeId, double distance) in startCandidates)
        {
            RouteGraphResolvedNode node = graph.GetNode(nodeId);
            System.Diagnostics.Debug.WriteLine(
                $"[NEW ALGORITHM]     {node.Route.Id} ({node.Route.Name}) " +
                $"{node.DirectionName} - {distance:F0}m");
        }

        System.Diagnostics.Debug.WriteLine(
            "[NEW ALGORITHM]   END CANDIDATES (near To):");

        foreach ((int nodeId, double distance) in endCandidates)
        {
            RouteGraphResolvedNode node = graph.GetNode(nodeId);
            System.Diagnostics.Debug.WriteLine(
                $"[NEW ALGORITHM]     {node.Route.Id} ({node.Route.Name}) " +
                $"{node.DirectionName} - {distance:F0}m");
        }

        List<Journey> journeys = new();

        if (startCandidates.Count == 0 || endCandidates.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine(
                "[NEW ALGORITHM]   No start or end candidates within " +
                "walking distance -- returning empty. If you expected " +
                "routes here, check RouteGraph's LogRouteSummary() output " +
                "to confirm those routes are even present in the graph " +
                "asset, and whether the pin is actually within " +
                $"{maximumWalkingDistanceMeters}m of any node on them.");

            queryTimer.Stop();
            return journeys;
        }

        List<(int StartNodeId, double StartDistance, int EndNodeId, double EndDistance)> sameRouteCandidates =
            startCandidates
                .Join(
                    endCandidates,
                    start => graph.GetNode(start.NodeId).Route,
                    end => graph.GetNode(end.NodeId).Route,
                    (start, end) =>
                        (start.NodeId, start.DistanceMeters, end.NodeId, end.DistanceMeters))
                .ToList();

        // Guarantee same-route direct journeys are always tried, even if
        // the generic distance-ranked pairing below would have crowded
        // them out. Two directions of the same route are never connected
        // to each other in the graph (in general they're genuinely
        // different streets) -- so when a route serves both From and To,
        // only ONE of its two directions is actually traversable in the
        // right order, and that direction isn't always the one with the
        // lowest summed walking distance. A few meters' difference in
        // walking distance can otherwise fill every top-K slot with the
        // WRONG direction's pairings before the right one ever gets a
        // chance, forcing Dijkstra into a nonsense detour through an
        // unrelated route instead of just riding this one directly.
        System.Diagnostics.Debug.WriteLine(
            $"[NEW ALGORITHM]   SAME-ROUTE CANDIDATES (guaranteed, not " +
            $"subject to the top-{MaximumCandidatePairsSearched} cutoff): " +
            $"{sameRouteCandidates.Count}");

        List<(int StartNodeId, double StartDistance, int EndNodeId, double EndDistance)> rankedPairs =
            startCandidates
                .SelectMany(start => endCandidates.Select(end =>
                    (start.NodeId, start.DistanceMeters, end.NodeId, end.DistanceMeters)))
                .OrderBy(pair => pair.Item2 + pair.Item4)
                .Take(MaximumCandidatePairsSearched)
                .ToList();

        List<(int StartNodeId, double StartDistance, int EndNodeId, double EndDistance)> prioritizedPairs =
            sameRouteCandidates
                .Concat(rankedPairs)
                .Distinct()
                .ToList();

        System.Diagnostics.Debug.WriteLine(
            $"[NEW ALGORITHM]   PRIORITIZED PAIRS: searching " +
            $"{prioritizedPairs.Count} ({sameRouteCandidates.Count} " +
            $"guaranteed same-route + up to {MaximumCandidatePairsSearched} " +
            $"ranked, {prioritizedPairs.Count - sameRouteCandidates.Count} " +
            $"of those ranked ones were new) of " +
            $"{startCandidates.Count * endCandidates.Count} possible pairs");

        HashSet<string> seenLegSequences = new();

        foreach ((int startNodeId, double startWalkingDistance, int endNodeId, double endWalkingDistance) in prioritizedPairs)
        {
            RouteGraphResolvedNode startNode = graph.GetNode(startNodeId);
            RouteGraphResolvedNode endNode = graph.GetNode(endNodeId);

            System.Diagnostics.Stopwatch pairTimer =
                System.Diagnostics.Stopwatch.StartNew();

            (List<int> Path, double TotalCost)? result =
                RunDijkstra(
                    startNodeId,
                    startWalkingDistance,
                    endNodeId,
                    endWalkingDistance,
                    maximumWalkingDistanceMeters);

            pairTimer.Stop();

            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NEW ALGORITHM]     PAIR {startNode.Route.Id}/" +
                    $"{startNode.DirectionName} -> {endNode.Route.Id}/" +
                    $"{endNode.DirectionName}: NO PATH FOUND " +
                    $"({pairTimer.ElapsedMilliseconds}ms)");
                continue;
            }

            Journey journey =
                ReconstructJourney(
                    result.Value.Path,
                    startWalkingDistance,
                    endWalkingDistance,
                    from,
                    to);

            if (journey.Legs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NEW ALGORITHM]     PAIR {startNode.Route.Id}/" +
                    $"{startNode.DirectionName} -> {endNode.Route.Id}/" +
                    $"{endNode.DirectionName}: path found but " +
                    $"reconstructed to zero legs (unexpected)");
                continue;
            }

            string? rejection = GetRejectionReason(journey);

            if (rejection != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NEW ALGORITHM]     PAIR {startNode.Route.Id}/" +
                    $"{startNode.DirectionName} -> {endNode.Route.Id}/" +
                    $"{endNode.DirectionName}: REJECTED ({rejection})");
                continue;
            }

            string legSummary =
                string.Join(
                    " -> ",
                    journey.Legs.Select(
                        leg =>
                            $"{leg.RouteId}/{leg.DirectionName}" +
                            $"(ride={leg.RideDistanceMeters:F0}m)"));

            string dedupeKey =
                string.Join(
                    "|",
                    journey.Legs.Select(
                        leg => $"{leg.RouteId}:{leg.DirectionName}"));

            bool isDuplicate = !seenLegSequences.Add(dedupeKey);

            System.Diagnostics.Debug.WriteLine(
                $"[NEW ALGORITHM]     PAIR {startNode.Route.Id}/" +
                $"{startNode.DirectionName} -> {endNode.Route.Id}/" +
                $"{endNode.DirectionName}: cost={result.Value.TotalCost:F0}s " +
                $"legs=[{legSummary}]" +
                (isDuplicate ? " -- SKIPPED (duplicate leg sequence)" : "") +
                $" ({pairTimer.ElapsedMilliseconds}ms)");

            if (isDuplicate)
            {
                continue;
            }

            journeys.Add(journey);
        }

        journeys = ApplyDominanceFilter(journeys);

        queryTimer.Stop();

        System.Diagnostics.Debug.WriteLine(
            $"[NEW ALGORITHM] TOTAL: {queryTimer.ElapsedMilliseconds}ms, " +
            $"{journeys.Count} journeys found");

        return journeys;
    }

    /// <summary>
    /// Estimated travel time in seconds: walking time plus riding time.
    /// Deliberately excludes the transfer penalty -- the minimum-savings
    /// rule compares real movement time, and the margin it requires
    /// already stands in for the cost of an extra transfer.
    /// </summary>
    private static double EstimateTravelSeconds(Journey journey)
    {
        return
            journey.TotalWalkingDistanceMeters / WalkSpeedMetersPerSecond +
            journey.TotalRideDistanceMeters / RideSpeedMetersPerSecond;
    }

    public static double EstimateTotalMinutes(Journey journey)
    {
        double seconds =
            EstimateTravelSeconds(journey) +
            journey.NumberOfTransfers * TransferPenaltySeconds;

        return seconds / 60.0;
    }

    /// <summary>
    /// Returns why a journey is impractical, or null if it is acceptable.
    /// </summary>
    private string? GetRejectionReason(Journey journey)
    {
        if (journey.Legs.Count > MaximumRides)
        {
            return $"{journey.Legs.Count} rides, max {MaximumRides}";
        }

        if (journey.Legs.Count > 1 &&
            journey.Legs.Any(
                leg => leg.RideDistanceMeters < MinimumRideMetersInMultiRide))
        {
            return $"a ride under {MinimumRideMetersInMultiRide:F0} m";
        }

        return null;
    }

    /// <summary>
    /// Keeps a journey only if no simpler option (fewer rides) is within
    /// MinimumSavingsSeconds of it. Journeys with the fewest rides found
    /// are always kept.
    /// </summary>
    private List<Journey> ApplyDominanceFilter(List<Journey> journeys)
    {
        List<Journey> kept = new();

        foreach (Journey journey in journeys)
        {
            List<Journey> simpler =
                journeys
                    .Where(other => other.Legs.Count < journey.Legs.Count)
                    .ToList();

            if (simpler.Count == 0)
            {
                kept.Add(journey);
                continue;
            }

            double bestSimplerSeconds =
                simpler.Min(other => EstimateTravelSeconds(other));

            if (EstimateTravelSeconds(journey) <=
                bestSimplerSeconds - MinimumSavingsSeconds)
            {
                kept.Add(journey);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NEW ALGORITHM]   DROPPED ({journey.Legs.Count} rides, " +
                    $"not {MinimumSavingsSeconds / 60:F0}+ min faster than " +
                    $"the best simpler option): " +
                    string.Join(
                        "|",
                        journey.Legs.Select(
                            leg => $"{leg.RouteId}:{leg.DirectionName}")));
            }
        }

        return kept;
    }

    /// <summary>
    /// Picks the single best journey: least walking distance, then
    /// fewest transfers, then shortest ride distance.
    /// </summary>
    public Journey? SelectBestJourney(List<Journey> journeys)
    {
        return journeys
            .OrderBy(j => j.TotalWalkingDistanceMeters)
            .ThenBy(j => j.NumberOfTransfers)
            .ThenBy(j => j.TotalRideDistanceMeters)
            .FirstOrDefault();
    }

    /// <summary>
    /// Top N journeys by total commute (walk + ride distance, since
    /// wait time is deliberately out of scope for this MVP).
    /// </summary>
    public List<Journey> TopByTotalCommute(List<Journey> journeys, int count)
    {
        return journeys
            .OrderBy(j => j.TotalTravelDistanceMeters)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Top N journeys by total walking distance only.
    /// </summary>
    public List<Journey> TopByWalkingDistance(List<Journey> journeys, int count)
    {
        return journeys
            .OrderBy(j => j.TotalWalkingDistanceMeters)
            .Take(count)
            .ToList();
    }

    private (List<int> Path, double TotalCost)? RunDijkstra(
        int startNodeId,
        double startWalkingDistance,
        int endNodeId,
        double endWalkingDistance,
        double maximumWalkingDistanceMeters)
    {
        int virtualOrigin = graph.NodeCount;
        int virtualDestination = graph.NodeCount + 1;
        int totalNodeSpace = graph.NodeCount + 2;

        double[] distances = new double[totalNodeSpace];
        Array.Fill(distances, double.MaxValue);

        int[] previous = new int[totalNodeSpace];
        Array.Fill(previous, -1);

        bool[] visited = new bool[totalNodeSpace];

        PriorityQueue<int, double> queue = new();

        distances[virtualOrigin] = 0;
        queue.Enqueue(virtualOrigin, 0);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (visited[current])
            {
                continue;
            }

            visited[current] = true;

            if (current == virtualDestination)
            {
                break;
            }

            foreach ((int neighbor, double weight) in GetNeighbors(
                current,
                startNodeId,
                startWalkingDistance,
                endNodeId,
                endWalkingDistance,
                maximumWalkingDistanceMeters,
                virtualOrigin,
                virtualDestination))
            {
                if (visited[neighbor])
                {
                    continue;
                }

                double candidateDistance = distances[current] + weight;

                if (candidateDistance < distances[neighbor])
                {
                    distances[neighbor] = candidateDistance;
                    previous[neighbor] = current;
                    queue.Enqueue(neighbor, candidateDistance);
                }
            }
        }

        if (distances[virtualDestination] == double.MaxValue)
        {
            return null;
        }

        List<int> path = new();
        int node = virtualDestination;

        while (node != -1)
        {
            path.Add(node);
            node = previous[node];
        }

        path.Reverse();

        return (path, distances[virtualDestination]);
    }

    /// <summary>
    /// Edge weights are in SECONDS: walking edges (the first walk, the
    /// last walk, and transfers) use walking speed, ride edges use
    /// riding speed, and transfers also pay TransferPenaltySeconds.
    /// </summary>
    private IEnumerable<(int To, double Weight)> GetNeighbors(
        int nodeId,
        int startNodeId,
        double startWalkingDistance,
        int endNodeId,
        double endWalkingDistance,
        double maximumWalkingDistanceMeters,
        int virtualOrigin,
        int virtualDestination)
    {
        if (nodeId == virtualOrigin)
        {
            yield return (
                startNodeId,
                startWalkingDistance / WalkSpeedMetersPerSecond);

            yield break;
        }

        if (nodeId == virtualDestination)
        {
            yield break;
        }

        foreach (RouteGraphEdge edge in graph.GetEdges(nodeId))
        {
            if (edge.IsTransfer &&
                edge.DistanceMeters > maximumWalkingDistanceMeters)
            {
                continue;
            }

            double weight =
                edge.IsTransfer
                    ? edge.DistanceMeters / WalkSpeedMetersPerSecond +
                      TransferPenaltySeconds
                    : edge.DistanceMeters / RideSpeedMetersPerSecond;

            yield return (edge.ToNode, weight);
        }

        if (nodeId == endNodeId)
        {
            yield return (
                virtualDestination,
                endWalkingDistance / WalkSpeedMetersPerSecond);
        }
    }

    private Journey ReconstructJourney(
        List<int> fullPath,
        double startWalkingDistance,
        double endWalkingDistance,
        GeoPoint originPoint,
        GeoPoint destinationPoint)
    {
        int virtualOrigin = graph.NodeCount;
        int virtualDestination = graph.NodeCount + 1;

        List<int> realNodes =
            fullPath
                .Where(id => id != virtualOrigin && id != virtualDestination)
                .ToList();

        Journey journey = new();

        if (realNodes.Count == 0)
        {
            return journey;
        }

        int legStart = 0;

        for (int i = 1; i <= realNodes.Count; i++)
        {
            bool endOfPath = i == realNodes.Count;

            bool directionChanged =
                !endOfPath &&
                (
                    graph.GetNode(realNodes[i]).Route !=
                        graph.GetNode(realNodes[legStart]).Route ||
                    graph.GetNode(realNodes[i]).DirectionName !=
                        graph.GetNode(realNodes[legStart]).DirectionName
                );

            if (endOfPath || directionChanged)
            {
                int legEndIndex = i - 1;

                bool isFirstLeg = journey.Legs.Count == 0;
                bool isLastLeg = endOfPath;

                double walkingDistanceIntoLeg =
                    isFirstLeg
                        ? startWalkingDistance
                        : GetEdgeWeight(
                            realNodes[legStart - 1],
                            realNodes[legStart]);

                GeoPoint walkFromPoint =
                    isFirstLeg
                        ? originPoint
                        : graph.GetNode(realNodes[legStart]).Point;

                GeoPoint walkToPoint =
                    isLastLeg
                        ? destinationPoint
                        : graph.GetNode(realNodes[i]).Point;

                journey.Legs.Add(
                    BuildLeg(
                        realNodes,
                        legStart,
                        legEndIndex,
                        walkingDistanceIntoLeg,
                        isLastLeg ? endWalkingDistance : 0,
                        walkFromPoint,
                        walkToPoint));

                legStart = i;
            }
        }

        return journey;
    }

    private JourneyLeg BuildLeg(
        List<int> realNodes,
        int legStart,
        int legEndIndex,
        double fromWalkingDistance,
        double toWalkingDistance,
        GeoPoint walkFromPoint,
        GeoPoint walkToPoint)
    {
        RouteGraphResolvedNode firstNode = graph.GetNode(realNodes[legStart]);
        RouteGraphResolvedNode lastNode = graph.GetNode(realNodes[legEndIndex]);

        double rideDistance = 0;

        for (int i = legStart; i < legEndIndex; i++)
        {
            rideDistance += GetRideEdgeWeight(realNodes[i], realNodes[i + 1]);
        }

        DirectionEvaluation evaluation = new()
        {
            Route = firstNode.Route,
            DirectionName = firstNode.DirectionName,
            Direction = firstNode.Direction,
            From = walkFromPoint,
            To = walkToPoint,
            NearestFrom = firstNode.Point,
            NearestTo = lastNode.Point,
            FromIndex = firstNode.PathIndex,
            ToIndex = lastNode.PathIndex,
            FromWalkingDistance = fromWalkingDistance,
            ToWalkingDistance = toWalkingDistance,
            TotalWalkingDistance = fromWalkingDistance + toWalkingDistance,
            RideDistanceMeters = rideDistance,
            Viable = true
        };

        return new JourneyLeg { Evaluation = evaluation };
    }

    /// <summary>
    /// Real distance in meters of the edge between two nodes (not the
    /// search weight, which is in seconds and includes the transfer
    /// penalty). Used when reconstructing the walking distance that led
    /// INTO a leg, and for ride distances.
    /// </summary>
    private double GetEdgeWeight(int fromNode, int toNode)
    {
        foreach (RouteGraphEdge edge in graph.GetEdges(fromNode))
        {
            if (edge.ToNode == toNode)
            {
                return edge.DistanceMeters;
            }
        }

        throw new InvalidOperationException(
            $"No edge found from node {fromNode} to node {toNode} " +
            "during journey reconstruction (walking distance lookup).");
    }

    /// <summary>
    /// Distance in meters of a RIDE edge specifically. Same lookup as
    /// GetEdgeWeight, named separately so the two call sites read
    /// clearly for what they mean.
    /// </summary>
    private double GetRideEdgeWeight(int fromNode, int toNode)
    {
        return GetEdgeWeight(fromNode, toNode);
    }
}