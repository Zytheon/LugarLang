using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

public class TransitRouteState
{
    public Route Route { get; set; } = null!;

    public Direction Direction { get; set; } = null!;

    public int PathIndex { get; set; }

    public double WalkingDistanceMeters { get; set; }

    public double RideDistanceMeters { get; set; }

    public int NumberOfTransfers { get; set; }

    public TransitRouteState? PreviousState { get; set; }
}