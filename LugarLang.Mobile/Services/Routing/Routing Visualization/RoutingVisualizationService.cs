using System.Text;
using LugarLang.Mobile.Services.Routing;

namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingVisualizationService
{
    private readonly RoutingCandidateService routingCandidateService;

    public RoutingVisualizationService(
        RoutingCandidateService routingCandidateService)
    {
        this.routingCandidateService =
            routingCandidateService;
    }

    public RoutingDebugSnapshot CreateSnapshot(
        IEnumerable<DirectionEvaluation> candidates,
        DirectionEvaluation? selectedCandidate,
        double maximumWalkingDistanceMeters)
    {
        List<DirectionEvaluation> selectedCandidates =
            routingCandidateService.SelectCandidates(
                candidates,
                maximumWalkingDistanceMeters);

        List<DirectionEvaluation> walkRanked =
            selectedCandidates
                .OrderBy(
                    candidate =>
                        candidate.TotalWalkingDistance)
                .ToList();

        List<DirectionEvaluation> rideRanked =
            selectedCandidates
                .OrderBy(
                    candidate =>
                        candidate.RideDistanceMeters)
                .ToList();

        Dictionary<DirectionEvaluation, int> walkRanks =
            new();

        for (
            int i = 0;
            i < walkRanked.Count;
            i++)
        {
            walkRanks[walkRanked[i]] =
                i + 1;
        }

        Dictionary<DirectionEvaluation, int> rideRanks =
            new();

        for (
            int i = 0;
            i < rideRanked.Count;
            i++)
        {
            rideRanks[rideRanked[i]] =
                i + 1;
        }

        RoutingDebugSnapshot snapshot =
            new()
            {
                MaximumWalkingDistanceMeters =
                    maximumWalkingDistanceMeters,

                SelectedCandidate =
                    selectedCandidate == null
                        ? null
                        : ConvertToDebugInfo(
                            selectedCandidate,
                            maximumWalkingDistanceMeters,
                            walkRanks,
                            rideRanks)
            };

        foreach (
            DirectionEvaluation candidate
            in selectedCandidates)
        {
            snapshot.Candidates.Add(
                ConvertToDebugInfo(
                    candidate,
                    maximumWalkingDistanceMeters,
                    walkRanks,
                    rideRanks));
        }

        return snapshot;
    }

    public RoutingDebugInfo ConvertJourneyToDebugInfo(
        Journey journey,
        double maximumWalkingDistanceMeters)
    {
        RoutingDebugInfo debugInfo =
            new()
            {
                JourneyId =
                    BuildJourneyId(journey),

                TotalWalkingDistance =
                    journey.TotalWalkingDistanceMeters,

                RideDistanceMeters =
                    journey.TotalRideDistanceMeters,

                NumberOfRides =
                    journey.NumberOfRides,

                NumberOfTransfers =
                    journey.NumberOfTransfers,

                Viable =
                    true,

                Score =
                    0
            };

        foreach (
            JourneyLeg leg
            in journey.Legs)
        {
            debugInfo.Legs.Add(
                new RoutingDebugLegInfo
                {
                    RouteId =
                        leg.RouteId,

                    RouteName =
                        leg.RouteName,

                    DirectionName =
                        leg.DirectionName,

                    FromWalkingDistance =
                        leg.FromWalkingDistanceMeters,

                    ToWalkingDistance =
                        leg.ToWalkingDistanceMeters,

                    RideDistanceMeters =
                        leg.RideDistanceMeters
                });
        }

        return debugInfo;
    }

    public string CreateTextSummary(
        RoutingDebugSnapshot snapshot)
    {
        StringBuilder output =
            new();

        output.AppendLine(
            "========== ROUTING ANALYSIS ==========");

        output.AppendLine(
            $"Walking preference: " +
            $"{snapshot.MaximumWalkingDistanceMeters:F0} m");

        output.AppendLine();

        output.AppendLine(
            $"Candidates shown: " +
            $"{snapshot.Candidates.Count}");

        output.AppendLine();

        foreach (
            RoutingDebugInfo candidate
            in snapshot.Candidates)
        {
            output.AppendLine(
                $"JOURNEY {candidate.JourneyId}");

            for (
                int i = 0;
                i < candidate.Legs.Count;
                i++)
            {
                RoutingDebugLegInfo leg =
                    candidate.Legs[i];

                output.AppendLine(
                    $"  RIDE {i + 1}: " +
                    $"{leg.RouteId} | " +
                    $"{leg.RouteName}");

                output.AppendLine(
                    $"    Direction: " +
                    $"{leg.DirectionName}");

                output.AppendLine(
                    $"    From walk: " +
                    $"{leg.FromWalkingDistance:F0} m");

                output.AppendLine(
                    $"    Ride: " +
                    $"{leg.RideDistanceMeters:F0} m");

                output.AppendLine(
                    $"    To walk: " +
                    $"{leg.ToWalkingDistance:F0} m");
            }

            output.AppendLine(
                $"Total walk: " +
                $"{candidate.TotalWalkingDistance:F0} m");

            output.AppendLine(
                $"Total ride: " +
                $"{candidate.RideDistanceMeters:F0} m");

            output.AppendLine(
                $"Rides: " +
                $"{candidate.NumberOfRides}");

            output.AppendLine(
                $"Transfers: " +
                $"{candidate.NumberOfTransfers}");

            output.AppendLine();

        }

        output.AppendLine(
            "====================================");

        return output.ToString();
    }

    private RoutingDebugInfo ConvertToDebugInfo(
        DirectionEvaluation evaluation,
        double maximumWalkingDistanceMeters,
        Dictionary<DirectionEvaluation, int> walkRanks,
        Dictionary<DirectionEvaluation, int> rideRanks)
    {
        bool directionCorrect =
            evaluation.ToIndex >
            evaluation.FromIndex;

        RoutingDebugInfo debugInfo =
            new()
            {
                JourneyId =
                    evaluation.Route.Id,

                TotalWalkingDistance =
                    evaluation.TotalWalkingDistance,

                RideDistanceMeters =
                    evaluation.RideDistanceMeters,

                NumberOfRides =
                    1,

                NumberOfTransfers =
                    0,

                Viable =
                    evaluation.Viable,

                Score =
                    0,

                WalkPreferenceRank =
                    walkRanks.TryGetValue(
                        evaluation,
                        out int walkRank)
                        ? walkRank
                        : 0,

                RidePreferenceRank =
                    rideRanks.TryGetValue(
                        evaluation,
                        out int rideRank)
                        ? rideRank
                        : 0
            };

        debugInfo.Legs.Add(
            new RoutingDebugLegInfo
            {
                RouteId =
                    evaluation.Route.Id,

                RouteName =
                    evaluation.Route.Name,

                DirectionName =
                    evaluation.DirectionName,

                FromWalkingDistance =
                    evaluation.FromWalkingDistance,

                ToWalkingDistance =
                    evaluation.ToWalkingDistance,

                RideDistanceMeters =
                    evaluation.RideDistanceMeters
            });

        return debugInfo;
    }

    private string BuildJourneyId(
        Journey journey)
    {
        return string.Join(
            " → ",
            journey.Legs.Select(
                leg =>
                    $"{leg.RouteId}:{leg.DirectionName}"));
    }
}