namespace LugarLang.Mobile.Services.RoutingVisualization;

public static class JourneyPresentation
{
    private static readonly (int R, int G, int B)[] RideColors =
    {
        (26, 115, 232),   // blue
        (232, 113, 10),   // orange
        (147, 52, 230),   // purple
        (216, 27, 96)     // pink
    };

    public static (int R, int G, int B) GetRideRgb(int rideIndex)
    {
        return RideColors[rideIndex % RideColors.Length];
    }

    // "Centro Baikingon to Carmen Public Market" -> "toward Carmen Public Market"
    public static string DirectionLabel(string? summary, string fallback)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return fallback;
        }

        int index = summary.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return fallback;
        }

        string destination = summary[(index + 4)..].Trim();

        return destination.Length == 0
            ? fallback
            : $"toward {destination}";
    }

    public static string FormatDistance(double meters)
    {
        return meters >= 1000
            ? $"{meters / 1000:F1} km"
            : $"{meters:F0} m";
    }
    public static string FormatKilometers(double meters)
    {
        return $"{meters / 1000:F1} km";
    }
}