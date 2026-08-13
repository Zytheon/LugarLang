using CdoGtfsConverter.Models;
namespace LugarLang.Mobile.Services.Routing;

public class StraightLineWalkingRouter : IWalkingRouter
{
    private readonly double maximumWalkingDistanceMeters;
    public StraightLineWalkingRouter(
        double maximumWalkingDistanceMeters)
    {
        this.maximumWalkingDistanceMeters =
            maximumWalkingDistanceMeters;
    }

    public WalkingRouteResult FindRoute(
        GeoPoint start,
        GeoPoint end)
    {
        double distanceMeters =
            CalculateDistanceMeters(
                start.Latitude,
                start.Longitude,
                end.Latitude,
                end.Longitude);

        return new WalkingRouteResult
        {
            Start = start,
            End = end,
            DistanceMeters = distanceMeters,
            IsReachable =
                distanceMeters <=
                maximumWalkingDistanceMeters,

            Path = new List<GeoPoint>
        {
            start,
            end
        }
        };
    }

    private double CalculateDistanceMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadius = 6371000.0;

        double dLat =
            DegreesToRadians(lat2 - lat1);

        double dLon =
            DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) *
            Math.Sin(dLat / 2) +
            Math.Cos(
                DegreesToRadians(lat1)) *
            Math.Cos(
                DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private double DegreesToRadians(
        double degrees)
    {
        return degrees *
               Math.PI /
               180.0;
    }

}

