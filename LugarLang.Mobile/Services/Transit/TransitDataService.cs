using CdoGtfsConverter.Importers;
using CdoGtfsConverter.Models;

namespace LugarLang.Mobile.Services.Transit;

public class TransitDataService
{
    public TransportNetwork Network { get; }

    public TransitDataService()
    {
        JsonImporter importer = new();

        string filePath =
            CopyRoutesFile();

        Network =
            importer.Import(filePath);
    }


    private string CopyRoutesFile()
    {
        string target =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "routes.json");


        if (!File.Exists(target))
        {
            using Stream input =
                FileSystem
                    .OpenAppPackageFileAsync("routes.json")
                    .Result;


            using FileStream output =
                File.Create(target);


            input.CopyTo(output);
        }


        return target;
    }
}
