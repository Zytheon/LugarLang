using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

public class DirectionEvaluation
{
    public Route Route { get; set; } = null!;

    public string DirectionName { get; set; } = "";

    public Direction Direction { get; set; } = null!;

    public GeoPoint From { get; set; } = null!;

    public GeoPoint To { get; set; } = null!;

    public GeoPoint NearestFrom { get; set; } = null!;

    public GeoPoint NearestTo { get; set; } = null!;

    public int FromIndex { get; set; }

    public int ToIndex { get; set; }

    public double FromWalkingDistance { get; set; }

    public double ToWalkingDistance { get; set; }

    public double TotalWalkingDistance { get; set; }

    public double RideDistanceMeters { get; set; }

    public bool Viable { get; set; }
}
