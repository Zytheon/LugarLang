using System.Text.Json;

namespace LugarLang.Mobile.Services.RoutingSpatial;

/// <summary>
/// Loads the precomputed routegraph.json asset that ships bundled with the
/// APK. This performs no graph-building work -- it only copies the packaged
/// asset out to app storage (same pattern TransitDataService uses for
/// routes.json) and deserializes it. All the expensive work (sampling,
/// ride-edge distances, transfer-edge discovery) already happened offline
/// in the build_graph.py precompute step.
/// </summary>
public class RouteGraphLoader
{
    private const string AssetFileName = "routegraph.json";

    public RouteGraphAsset Load()
    {
        string filePath = CopyAssetFile();

        using FileStream stream = File.OpenRead(filePath);

        RouteGraphAsset? asset =
            JsonSerializer.Deserialize<RouteGraphAsset>(stream);

        if (asset == null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize {AssetFileName}: result was null.");
        }

        return asset;
    }

    private string CopyAssetFile()
    {
        string target =
            Path.Combine(
                FileSystem.AppDataDirectory,
                AssetFileName);

        if (!File.Exists(target))
        {
            using Stream input =
                FileSystem
                    .OpenAppPackageFileAsync(AssetFileName)
                    .Result;

            using FileStream output =
                File.Create(target);

            input.CopyTo(output);
        }

        return target;
    }
}