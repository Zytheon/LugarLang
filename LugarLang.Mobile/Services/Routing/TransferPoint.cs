using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

public class TransferPoint
{
    public GeoPoint Location { get; set; } = null!;

    public double FirstRouteDistanceFromStartMeters { get; set; }

    public double SecondRouteDistanceFromStartMeters { get; set; }

    public double WalkingDistanceMeters { get; set; }

    public int FirstRoutePathIndex { get; set; }

    public int SecondRoutePathIndex { get; set; }
}
