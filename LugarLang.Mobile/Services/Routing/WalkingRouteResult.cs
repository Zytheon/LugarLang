using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

public class WalkingRouteResult
{
    public GeoPoint Start { get; set; } = null!;

    public GeoPoint End { get; set; } = null!;

    public double DistanceMeters { get; set; }

    public bool IsReachable { get; set; }

    public List<GeoPoint> Path { get; set; } = new();
}
