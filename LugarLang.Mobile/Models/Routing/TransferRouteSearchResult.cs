using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Models;



public class TransferRouteSearchResult
{
    public bool Found { get; set; }

    public RouteSearchResult FirstRide { get; set; } = new();

    public RouteSearchResult SecondRide { get; set; } = new();

    public Stop? TransferStop { get; set; }

    public int TotalStops { get; set; }
}
