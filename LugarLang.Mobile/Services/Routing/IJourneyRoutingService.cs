using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

/// <summary>
/// Common contract for anything that can answer "how do I get from A to B".
/// Lets MapPage hold either JourneyRoutingService (the existing live
/// nearest-point-search algorithm) or SegmentGraphJourneyRoutingService
/// (the precomputed-graph alternative) behind the same field, and swap
/// between them at runtime for comparison.
/// </summary>
public interface IJourneyRoutingService
{
    List<Journey> FindJourneys(
        IEnumerable<Route> routes,
        GeoPoint from,
        GeoPoint to,
        double maximumWalkingDistanceMeters);
}