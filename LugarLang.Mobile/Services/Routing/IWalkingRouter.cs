using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Routing;

public interface IWalkingRouter
{
    WalkingRouteResult FindRoute(
        GeoPoint start,
        GeoPoint end);
}
