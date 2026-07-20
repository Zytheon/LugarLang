using CdoGtfsConverter.Models;
using LugarLang.Mobile.Models;

namespace LugarLang.Mobile.Services;

public class TransferRouteSearchService
{
    private readonly TransportNetwork network;
    private readonly RouteSearchService routeSearchService;

    public TransferRouteSearchService(
        TransportNetwork network)
    {
        this.network = network;
        routeSearchService = new RouteSearchService(network);
    }


    public TransferRouteSearchResult Search(
        string from,
        string to)
    {
        TransferRouteSearchResult? bestResult = null;


        foreach (Route route in network.Routes)
        {
            TransferRouteSearchResult? result =
                TryRoute(
                    route.Id,
                    route.Inbound,
                    from,
                    to);


            if (result != null)
            {
                if (bestResult == null ||
                    result.TotalStops < bestResult.TotalStops)
                {
                    bestResult = result;
                }
            }


            result =
                TryRoute(
                    route.Id,
                    route.Outbound,
                    from,
                    to);


            if (result != null)
            {
                if (bestResult == null ||
                    result.TotalStops < bestResult.TotalStops)
                {
                    bestResult = result;
                }
            }
        }


        return bestResult ??
            new TransferRouteSearchResult
            {
                Found = false
            };
    }



    private TransferRouteSearchResult? TryRoute(
        string routeId,
        Direction? direction,
        string from,
        string to)
    {
        if (direction == null)
            return null;


        int fromIndex = direction.Stops.FindIndex(
            stop => stop.Name.Contains(
                from,
                StringComparison.OrdinalIgnoreCase));


        if (fromIndex == -1)
            return null;



        TransferRouteSearchResult? bestTransfer = null;



        for (int i = fromIndex; i < direction.Stops.Count; i++)
        {
            Stop transferStop = direction.Stops[i];


            RouteSearchResult secondRide =
                routeSearchService.Search(
                    transferStop.Name,
                    to);


            if (!secondRide.Found)
                continue;



            List<Stop> firstRideStops =
                direction.Stops
                    .Skip(fromIndex)
                    .Take(i - fromIndex + 1)
                    .ToList();



            RouteSearchResult firstRide =
                new RouteSearchResult
                {
                    Found = true,
                    RouteId = routeId,
                    Direction = direction.Summary,
                    BoardingStop = direction.Stops[fromIndex],
                    DestinationStop = transferStop,
                    Stops = firstRideStops
                };



            TransferRouteSearchResult candidate =
                new TransferRouteSearchResult
                {
                    Found = true,
                    FirstRide = firstRide,
                    SecondRide = secondRide,
                    TransferStop = transferStop,

                    TotalStops =
                        firstRide.Stops.Count +
                        secondRide.Stops.Count
                };



            if (bestTransfer == null ||
                candidate.TotalStops < bestTransfer.TotalStops)
            {
                bestTransfer = candidate;
            }
        }


        return bestTransfer;
    }
}
