using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services;

public class TransferDiagnosticsService
{
    private readonly TransportNetwork network;

    public TransferDiagnosticsService(
        TransportNetwork network)
    {
        this.network = network;
    }

    public List<(string RouteId, string Direction)> FindRoutesAtStop(
        string stopName)
    {
        List<(string RouteId, string Direction)> routes = new();

        foreach (Route route in network.Routes)
        {
            if (route.Inbound != null &&
                route.Inbound.Stops.Any(
                    s => s.Name.Contains(
                        stopName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                routes.Add((route.Id, "Inbound"));
            }

            if (route.Outbound != null &&
                route.Outbound.Stops.Any(
                    s => s.Name.Contains(
                        stopName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                routes.Add((route.Id, "Outbound"));
            }
        }

        return routes;
    }
}
