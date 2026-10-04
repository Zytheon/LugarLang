using System.Text.Json.Serialization;

namespace LugarLang.Mobile.Services.RoutingSpatial;

/// <summary>
/// Raw deserialized shape of the precomputed routegraph.json asset.
/// This is intentionally a thin data holder -- resolving nodes into real
/// GeoPoints (from the already-loaded TransportNetwork) and building
/// adjacency lists happens in RouteGraph, not here.
/// </summary>
public class RouteGraphAsset
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("sampleSpacingMeters")]
    public double SampleSpacingMeters { get; set; }

    [JsonPropertyName("maxTransferDistanceMeters")]
    public double MaxTransferDistanceMeters { get; set; }

    [JsonPropertyName("nodes")]
    public List<RouteGraphNodeAsset> Nodes { get; set; } = new();

    [JsonPropertyName("rideEdges")]
    public List<double[]> RideEdges { get; set; } = new();

    [JsonPropertyName("transferEdges")]
    public List<double[]> TransferEdges { get; set; } = new();
}

public class RouteGraphNodeAsset
{
    [JsonPropertyName("routeId")]
    public string RouteId { get; set; } = "";

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "";

    [JsonPropertyName("pathIndex")]
    public int PathIndex { get; set; }
}