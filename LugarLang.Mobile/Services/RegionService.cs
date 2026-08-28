namespace LugarLang.Mobile.Services;

public class RegionService
{
    public string? GetRegion(
        double latitude,
        double longitude)
    {
        // Northern Mindanao
        //
        // This currently covers the general
        // Northern Mindanao area rather than
        // attempting precise provincial boundaries.
        if (
            latitude >= 7.0 &&
            latitude <= 9.5 &&
            longitude >= 123.5 &&
            longitude <= 126.5)
        {
            return "Northern Mindanao";
        }

        // Central Visayas
        if (
            latitude >= 9.0 &&
            latitude <= 11.5 &&
            longitude >= 123.0 &&
            longitude <= 125.0)
        {
            return "Central Visayas";
        }

        // Davao Region
        if (
            latitude >= 5.0 &&
            latitude <= 8.5 &&
            longitude >= 124.5 &&
            longitude <= 126.5)
        {
            return "Davao Region";
        }

        // Metro Manila
        if (
            latitude >= 14.3 &&
            latitude <= 14.9 &&
            longitude >= 120.8 &&
            longitude <= 121.3)
        {
            return "Metro Manila";
        }

        // Western Visayas
        if (
            latitude >= 10.0 &&
            latitude <= 12.5 &&
            longitude >= 121.0 &&
            longitude <= 123.0)
        {
            return "Western Visayas";
        }

        return null;
    }
}