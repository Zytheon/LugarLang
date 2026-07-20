using CdoGtfsConverter.Importers;
using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services;

public class TransitDataService
{
    public TransportNetwork Network { get; private set; } = null!;


    public TransitDataService()
    {
        Console.WriteLine("Loading transit data...");

        JsonImporter importer = new();

        Network = importer.Import("routes.json");

        Console.WriteLine("Transit data loaded!");
    }
}
