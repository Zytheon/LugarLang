namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingDebugInfo
{
    public string RouteId { get; set; } = "";

    public string RouteName { get; set; } = "";

    public string DirectionName { get; set; } = "";

    public double FromWalkingDistance { get; set; }

    public double ToWalkingDistance { get; set; }

    public double TotalWalkingDistance { get; set; }

    public double RideDistanceMeters { get; set; }

    public bool WithinWalkingPreference { get; set; }

    public bool DirectionCorrect { get; set; }

    public bool Viable { get; set; }

    public double Score { get; set; }

    public int WalkPreferenceRank { get; set; }

    public int RidePreferenceRank { get; set; }
}
