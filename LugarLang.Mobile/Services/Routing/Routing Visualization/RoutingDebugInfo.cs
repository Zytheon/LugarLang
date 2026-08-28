namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingDebugInfo
{
    public string JourneyId { get; set; } = "";

    public List<RoutingDebugLegInfo> Legs { get; set; } =
        new();

    public double FromWalkingDistance { get; set; }

    public double ToWalkingDistance { get; set; }

    public double TransferWalkingDistance { get; set; }

    public double TotalWalkingDistance { get; set; }

    public double RideDistanceMeters { get; set; }

    public int NumberOfRides { get; set; }

    public int NumberOfTransfers { get; set; }

    public bool Viable { get; set; }

    public double Score { get; set; }

    public int WalkPreferenceRank { get; set; }

    public int RidePreferenceRank { get; set; }

    public string RouteSummary
    {
        get
        {
            return string.Join(
                " → ",
                Legs.Select(
                    leg =>
                        $"{leg.RouteName} ({leg.DirectionName})"));
        }
    }
}

public class RoutingDebugLegInfo
{
    public string RouteId { get; set; } = "";

    public string RouteName { get; set; } = "";

    public string DirectionName { get; set; } = "";

    public double FromWalkingDistance { get; set; }

    public double ToWalkingDistance { get; set; }

    public double RideDistanceMeters { get; set; }
}