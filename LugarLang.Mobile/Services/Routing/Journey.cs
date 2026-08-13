using System.Collections.Generic;
using System.Linq;

namespace LugarLang.Mobile.Services.Routing;

public class Journey
{
    public List<JourneyLeg> Legs { get; set; } =
        new();

    public double TotalWalkingDistanceMeters
    {
        get
        {
            return Legs.Sum(
                leg =>
                    leg.FromWalkingDistanceMeters +
                    leg.ToWalkingDistanceMeters);
        }
    }

    public double TotalRideDistanceMeters
    {
        get
        {
            return Legs.Sum(
                leg =>
                    leg.RideDistanceMeters);
        }
    }

    public int NumberOfRides
    {
        get
        {
            return Legs.Count;
        }
    }

    public int NumberOfTransfers
    {
        get
        {
            return Math.Max(
                0,
                NumberOfRides - 1);
        }
    }

    public double TotalTravelDistanceMeters
    {
        get
        {
            return
                TotalWalkingDistanceMeters +
                TotalRideDistanceMeters;
        }
    }
}

public class JourneyLeg
{
    public DirectionEvaluation Evaluation { get; set; } = null!;

    public double FromWalkingDistanceMeters
    {
        get
        {
            return Evaluation.FromWalkingDistance;
        }
    }

    public double ToWalkingDistanceMeters
    {
        get
        {
            return Evaluation.ToWalkingDistance;
        }
    }

    public double RideDistanceMeters
    {
        get
        {
            return Evaluation.RideDistanceMeters;
        }
    }

    public string RouteId
    {
        get
        {
            return Evaluation.Route.Id;
        }
    }

    public string RouteName
    {
        get
        {
            return Evaluation.Route.Name;
        }
    }

    public string DirectionName
    {
        get
        {
            return Evaluation.DirectionName;
        }
    }
}
