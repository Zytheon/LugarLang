using System.Text;
using LugarLang.Mobile.Services.Routing;

namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingVisualizationService
{
    public RoutingDebugSnapshot CreateSnapshot(
        IEnumerable<DirectionEvaluation> candidates,
        DirectionEvaluation? selectedCandidate,
        double maximumWalkingDistanceMeters)
    {
        List<DirectionEvaluation> viableCandidates =
            candidates
                .Where(candidate => candidate.Viable)
                .Where(candidate =>
                    candidate.TotalWalkingDistance <=
                    maximumWalkingDistanceMeters)
                .ToList();

        List<DirectionEvaluation> nonDominatedCandidates =
            RemoveDominatedCandidates(
                viableCandidates);

        List<DirectionEvaluation> selectedCandidates =
            SelectRepresentativeCandidates(
                nonDominatedCandidates);

        List<DirectionEvaluation> walkRanked =
            selectedCandidates
                .OrderBy(candidate =>
                    candidate.TotalWalkingDistance)
                .ToList();

        List<DirectionEvaluation> rideRanked =
            selectedCandidates
                .OrderBy(candidate =>
                    candidate.RideDistanceMeters)
                .ToList();

        Dictionary<DirectionEvaluation, int> walkRanks =
            new();

        for (int i = 0; i < walkRanked.Count; i++)
        {
            walkRanks[walkRanked[i]] =
                i + 1;
        }

        Dictionary<DirectionEvaluation, int> rideRanks =
            new();

        for (int i = 0; i < rideRanked.Count; i++)
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

    private List<DirectionEvaluation>
        RemoveDominatedCandidates(
            List<DirectionEvaluation> candidates)
    {
        List<DirectionEvaluation> result =
            new();

        foreach (
            DirectionEvaluation candidate
            in candidates)
        {
            bool dominated =
                candidates.Any(
                    other =>
                        other != candidate &&
                        other.TotalWalkingDistance <=
                            candidate.TotalWalkingDistance &&
                        other.RideDistanceMeters <=
                            candidate.RideDistanceMeters &&
                        (
                            other.TotalWalkingDistance <
                                candidate.TotalWalkingDistance ||
                            other.RideDistanceMeters <
                                candidate.RideDistanceMeters
                        ));

            if (!dominated)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private List<DirectionEvaluation>
        SelectRepresentativeCandidates(
            List<DirectionEvaluation> candidates)
    {
        if (candidates.Count <= 5)
        {
            return candidates
                .OrderBy(candidate =>
                    candidate.TotalWalkingDistance)
                .ToList();
        }

        List<DirectionEvaluation> ordered =
            candidates
                .OrderBy(candidate =>
                    candidate.TotalWalkingDistance)
                .ToList();

        List<DirectionEvaluation> selected =
            new();

        AddIfMissing(
            selected,
            ordered.First());

        AddIfMissing(
            selected,
            ordered.Last());

        if (selected.Count < 5)
        {
            AddIfMissing(
                selected,
                ordered[ordered.Count / 2]);
        }

        if (selected.Count < 5)
        {
            AddIfMissing(
                selected,
                ordered[ordered.Count / 4]);
        }

        if (selected.Count < 5)
        {
            AddIfMissing(
                selected,
                ordered[
                    (ordered.Count * 3) / 4]);
        }

        if (selected.Count < 5)
        {
            foreach (
                DirectionEvaluation candidate
                in ordered)
            {
                AddIfMissing(
                    selected,
                    candidate);

                if (selected.Count == 5)
                {
                    break;
                }
            }
        }

        return selected
            .OrderBy(candidate =>
                candidate.TotalWalkingDistance)
            .ToList();
    }

    private void AddIfMissing(
        List<DirectionEvaluation> selected,
        DirectionEvaluation candidate)
    {
        if (!selected.Contains(candidate))
        {
            selected.Add(candidate);
        }
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
                $"ROUTE {candidate.RouteId} | " +
                $"{candidate.RouteName}");

            output.AppendLine(
                $"Direction: " +
                $"{(candidate.DirectionCorrect
                    ? "RIGHT"
                    : "WRONG")}");

            output.AppendLine(
                $"First walk: " +
                $"{candidate.FromWalkingDistance:F0} m");

            output.AppendLine(
                $"Ride: " +
                $"{candidate.RideDistanceMeters:F0} m");

            output.AppendLine(
                $"Second walk: " +
                $"{candidate.ToWalkingDistance:F0} m");

            output.AppendLine(
                $"Total walk: " +
                $"{candidate.TotalWalkingDistance:F0} m");

            output.AppendLine(
                $"Walk preference rank: " +
                $"#{candidate.WalkPreferenceRank}");

            output.AppendLine(
                $"Ride preference rank: " +
                $"#{candidate.RidePreferenceRank}");

            output.AppendLine(
                $"Within preference: " +
                $"{candidate.WithinWalkingPreference}");

            output.AppendLine(
                $"Viable: " +
                $"{candidate.Viable}");

            output.AppendLine();
        }

        output.AppendLine(
            "========== SELECTED TRIP ==========");

        if (snapshot.SelectedCandidate == null)
        {
            output.AppendLine(
                "No trip selected.");
        }
        else
        {
            RoutingDebugInfo selected =
                snapshot.SelectedCandidate;

            output.AppendLine(
                $"ROUTE {selected.RouteId} | " +
                $"{selected.RouteName}");

            output.AppendLine(
                $"Direction: " +
                $"{(selected.DirectionCorrect
                    ? "RIGHT"
                    : "WRONG")}");

            output.AppendLine(
                $"Total walk: " +
                $"{selected.TotalWalkingDistance:F0} m");

            output.AppendLine(
                $"Ride: " +
                $"{selected.RideDistanceMeters:F0} m");

            output.AppendLine(
                $"Walk preference rank: " +
                $"#{selected.WalkPreferenceRank}");

            output.AppendLine(
                $"Ride preference rank: " +
                $"#{selected.RidePreferenceRank}");
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

        return new RoutingDebugInfo
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

            TotalWalkingDistance =
                evaluation.TotalWalkingDistance,

            RideDistanceMeters =
                evaluation.RideDistanceMeters,

            WithinWalkingPreference =
                evaluation.TotalWalkingDistance <=
                maximumWalkingDistanceMeters,

            DirectionCorrect =
                directionCorrect,

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
    }
}
