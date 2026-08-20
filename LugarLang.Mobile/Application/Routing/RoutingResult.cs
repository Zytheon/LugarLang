using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.Application.Routing;

public class RoutingResult
{
    public List<DirectionEvaluation> Candidates { get; }

    public DirectionEvaluation? BestTrip { get; }

    public RoutingDebugSnapshot Snapshot { get; }

    public RoutingResult(
        List<DirectionEvaluation> candidates,
        DirectionEvaluation? bestTrip,
        RoutingDebugSnapshot snapshot)
    {
        Candidates = candidates;
        BestTrip = bestTrip;
        Snapshot = snapshot;
    }
}