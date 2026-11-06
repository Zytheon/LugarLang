using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

/// <summary>
/// A cluster of journeys that board and alight in roughly the same
/// physical place -- e.g. five different route IDs that all happen to
/// connect the same two corridors. Representative is the lowest-cost
/// member and is what gets shown as the group's headline; Members is
/// every journey in the group (Representative included), so nothing is
/// ever discarded, only organized.
/// </summary>
public class JourneyGroup
{
    public Journey Representative { get; set; } = null!;

    public List<Journey> Members { get; set; } = new();
}

/// <summary>
/// Groups journeys by where they board and alight, not by route ID or
/// name. Route-ID/name similarity doesn't reliably indicate physical
/// redundancy -- e.g. the five routes serving Xavier Heights <-> Cogon
/// share no naming pattern at all, while barangay routes that DO share a
/// naming pattern (dansolihon-, pigsag-an-, tagpangi-, ...) are genuinely
/// different destinations, not duplicates of each other. What actually
/// makes two journeys "the same option, different operator" is that they
/// start and end in the same physical spot.
///
/// This exists so RoutingCandidateView can show diverse OPTIONS (one row
/// per group) without discarding any individual route -- every member of
/// a group remains fully reachable/selectable, just nested under its
/// group instead of eating a top-level slot on its own.
/// </summary>
public class JourneyGroupingService
{
    /// <summary>
    /// How close two journeys' boarding points (and separately, their
    /// alighting points) must be to count as "the same place" for
    /// grouping purposes. Chosen as a starting point, not measured against
    /// real queries yet -- tune this once you've seen it against more than
    /// one From/To pair. A journey only joins an existing group if BOTH
    /// its boarding AND alighting point are within this distance of the
    /// group's representative -- sharing just one end isn't enough (two
    /// journeys can share a boarding corridor and still genuinely diverge
    /// at the destination).
    /// </summary>
    private const double GroupingRadiusMeters = 100.0;

    public List<JourneyGroup> Group(IEnumerable<Journey> journeys)
    {
        List<JourneyGroup> groups = new();

        foreach (Journey journey in journeys)
        {
            if (journey.Legs.Count == 0)
            {
                continue;
            }

            GeoPoint boardingPoint =
                journey.Legs.First().Evaluation.NearestFrom;

            GeoPoint alightingPoint =
                journey.Legs.Last().Evaluation.NearestTo;

            // Compared against the group's REPRESENTATIVE only, not every
            // member -- simpler than full single-linkage clustering, and
            // fine as long as groups stay small (a handful of near-
            // duplicate routes), but it means a long chain of
            // slightly-different journeys COULD end up split across two
            // groups even if the endpoints are each pairwise close. Worth
            // revisiting if that shows up in practice.
            JourneyGroup? match =
                groups.FirstOrDefault(group =>
                {
                    GeoPoint groupBoardingPoint =
                        group.Representative.Legs.First().Evaluation.NearestFrom;

                    GeoPoint groupAlightingPoint =
                        group.Representative.Legs.Last().Evaluation.NearestTo;

                    return
                        CalculateDistanceMeters(
                            boardingPoint, groupBoardingPoint) <=
                            GroupingRadiusMeters &&
                        CalculateDistanceMeters(
                            alightingPoint, groupAlightingPoint) <=
                            GroupingRadiusMeters;
                });

            if (match != null)
            {
                match.Members.Add(journey);

                if (SegmentGraphJourneyRoutingService.EstimateTotalMinutes(journey) <
                    SegmentGraphJourneyRoutingService.EstimateTotalMinutes(match.Representative))
                {
                    match.Representative = journey;
                }

                continue;
            }

            groups.Add(
                new JourneyGroup
                {
                    Representative = journey,
                    Members = new List<Journey> { journey }
                });
        }

        return groups;
    }

    private double CalculateDistanceMeters(GeoPoint a, GeoPoint b)
    {
        const double earthRadius = 6371000.0;

        double lat1 = a.Latitude * Math.PI / 180.0;
        double lat2 = b.Latitude * Math.PI / 180.0;
        double dLat = (b.Latitude - a.Latitude) * Math.PI / 180.0;
        double dLon = (b.Longitude - a.Longitude) * Math.PI / 180.0;

        double h =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return earthRadius * c;
    }
}