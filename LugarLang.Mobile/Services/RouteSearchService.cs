using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services;

public class RouteSearchService
{
    private readonly TransportNetwork network;


    public RouteSearchService(TransportNetwork network)
    {
        this.network = network;
    }


    public string Search(string from, string to)
    {
        foreach (Route route in network.Routes)
        {
            if (route.Inbound != null)
            {
                bool hasFrom =
                    route.Inbound.Stops.Any(
                        stop => stop.Name.Contains(
                            from,
                            StringComparison.OrdinalIgnoreCase));


                bool hasTo =
                    route.Inbound.Stops.Any(
                        stop => stop.Name.Contains(
                            to,
                            StringComparison.OrdinalIgnoreCase));


                if (hasFrom && hasTo)
                {
                    return BuildResult(
                        route,
                        "Inbound");
                }
            }



            if (route.Outbound != null)
            {
                bool hasFrom =
                    route.Outbound.Stops.Any(
                        stop => stop.Name.Contains(
                            from,
                            StringComparison.OrdinalIgnoreCase));


                bool hasTo =
                    route.Outbound.Stops.Any(
                        stop => stop.Name.Contains(
                            to,
                            StringComparison.OrdinalIgnoreCase));


                if (hasFrom && hasTo)
                {
                    return BuildResult(
                        route,
                        "Outbound");
                }
            }
        }


        return "No direct route found.";
    }



    private string BuildResult(
        Route route,
        string direction)
    {
        return
            $"Ride: {route.Id}\n\n" +
            $"Direction: {direction}\n\n" +
            "Direct route found.";
    }
}
