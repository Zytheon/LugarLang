using CdoGtfsConverter.Models;
using LugarLang.Mobile.Models;

namespace LugarLang.Mobile.Services;

public class RouteSearchService
{
    private readonly TransportNetwork network;

    public RouteSearchService(TransportNetwork network)
    {
        this.network = network;
    }

    public RouteSearchResult Search(string from, string to)
    {
        foreach (Route route in network.Routes)
        {
            RouteSearchResult? result =
                SearchDirection(
                    route.Id,
                    "Inbound",
                    route.Inbound,
                    from,
                    to);

            if (result != null)
                return result;

            result =
                SearchDirection(
                    route.Id,
                    "Outbound",
                    route.Outbound,
                    from,
                    to);

            if (result != null)
                return result;
        }

        return new RouteSearchResult
        {
            Found = false
        };
    }

    private RouteSearchResult? SearchDirection(
        string routeId,
        string direction,
        Direction? routeDirection,
        string from,
        string to)
    {
        if (routeDirection == null)
            return null;

        int fromIndex = routeDirection.Stops.FindIndex(
            stop => stop.Name.Contains(
                from,
                StringComparison.OrdinalIgnoreCase));

        int toIndex = routeDirection.Stops.FindIndex(
            stop => stop.Name.Contains(
                to,
                StringComparison.OrdinalIgnoreCase));

        if (fromIndex == -1 || toIndex == -1)
            return null;

        // Destination must come after boarding stop
        if (fromIndex > toIndex)
            return null;

        List<Stop> journeyStops =
            routeDirection.Stops
                .Skip(fromIndex)
                .Take(toIndex - fromIndex + 1)
                .ToList();

        return new RouteSearchResult
        {
            Found = true,
            RouteId = routeId,
            Direction = direction,
            BoardingStop = routeDirection.Stops[fromIndex],
            DestinationStop = routeDirection.Stops[toIndex],
            Stops = journeyStops
        };
    }
}
