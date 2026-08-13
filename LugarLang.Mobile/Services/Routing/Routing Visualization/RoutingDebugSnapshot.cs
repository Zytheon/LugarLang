namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingDebugSnapshot
{
    public List<RoutingDebugInfo> Candidates { get; set; } = new();

    public RoutingDebugInfo? SelectedCandidate { get; set; }

    public double MaximumWalkingDistanceMeters { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
