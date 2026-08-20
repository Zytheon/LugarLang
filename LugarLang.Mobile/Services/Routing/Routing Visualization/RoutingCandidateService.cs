using System.Linq;

namespace LugarLang.Mobile.Services.Routing;

public class RoutingCandidateService
{
    public List<DirectionEvaluation> SelectCandidates(
        IEnumerable<DirectionEvaluation> candidates,
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

        return SelectRepresentativeCandidates(
            nonDominatedCandidates);
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
}



