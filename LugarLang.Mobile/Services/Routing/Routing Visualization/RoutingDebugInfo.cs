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
    public double EstimatedMinutes { get; set; }

public List<string> Badges { get; set; } = new();

    /// <summary>
    /// Other journeys grouped with this one by JourneyGroupingService
    /// (same physical boarding/alighting area, different route ID/name).
    /// This entry is always the group's cheapest member (the
    /// "representative") -- GroupMembers holds the OTHER members only,
    /// never a duplicate of this entry itself. Empty for an ungrouped
    /// journey or a journey that was the only one in its group.
    /// </summary>
    public List<RoutingDebugInfo> GroupMembers { get; set; } =
        new();

    public string RouteSummary
    {
        get
        {
            return string.Join(
                " → ",
                Legs.Select(
                    leg =>
                        $"{leg.RouteName} ({(string.IsNullOrEmpty(leg.DirectionLabel) ? leg.DirectionName : leg.DirectionLabel)})"));
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
    public string DirectionLabel { get; set; } = "";
}