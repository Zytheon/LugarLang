
using CdoGtfsConverter.Importers;
using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services;

public class TransitDataService
{
    public TransportNetwork Network { get; }

    public TransitDataService()
    {
        JsonImporter importer = new();

        Network = importer.Import("routes.json");
    }
}
