using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services;

public class AutocompleteService
{
    private readonly List<Stop> stops;


    public AutocompleteService(TransportNetwork network)
    {
        stops = network.Stops;
    }


    public List<string> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }


        return stops
            .Where(stop =>
                stop.Name.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase))
            .Select(stop => stop.Name)
            .Distinct()
            .Take(10)
            .ToList();
    }
}
