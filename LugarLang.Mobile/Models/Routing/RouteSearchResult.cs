using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Models;

public class RouteSearchResult
{
    public bool Found { get; set; }

    public string RouteId { get; set; } = "";

    public string Direction { get; set; } = "";

    // Where the passenger boards
    public Stop? BoardingStop { get; set; }

    // Where the passenger gets off
    public Stop? DestinationStop { get; set; }

    // Only the stops between boarding and destination (inclusive)
    public List<Stop> Stops { get; set; } = new();
}
