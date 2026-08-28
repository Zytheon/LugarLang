using Microsoft.Maui.Controls;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.UI.Routing;

public class RoutingCandidateView : ContentView
{
    private readonly VerticalStackLayout contentLayout;

    private RoutingDebugSnapshot? currentSnapshot;

    private enum SortMode
    {
        Walk,
        Ride
    }

    private SortMode currentSortMode =
        SortMode.Walk;

    public event EventHandler<RoutingDebugInfo>? CandidateSelected;

    public RoutingCandidateView()
    {
        contentLayout =
            new VerticalStackLayout
            {
                Spacing = 10,
                Padding = 12
            };

        Content =
            new ScrollView
            {
                Content = contentLayout
            };

        ShowMessage(
            "Set a From and To location.");
    }

    public void DisplayJourneys(
        IEnumerable<Journey> journeys,
        double maximumWalkingDistanceMeters)
    {
        List<Journey> journeyList =
            SelectRepresentativeJourneys(
                journeys.ToList());

        currentSnapshot =
            new RoutingDebugSnapshot
            {
                MaximumWalkingDistanceMeters =
                    maximumWalkingDistanceMeters
            };

        List<RoutingDebugInfo> debugCandidates =
            journeyList
                .Select(
                    journey =>
                        CreateDebugInfo(
                            journey,
                            maximumWalkingDistanceMeters))
                .ToList();

        List<RoutingDebugInfo> walkRanked =
            debugCandidates
                .OrderBy(
                    candidate =>
                        candidate.TotalWalkingDistance)
                .ToList();

        List<RoutingDebugInfo> rideRanked =
            debugCandidates
                .OrderBy(
                    candidate =>
                        candidate.RideDistanceMeters)
                .ToList();

        for (
            int i = 0;
            i < walkRanked.Count;
            i++)
        {
            walkRanked[i].WalkPreferenceRank =
                i + 1;
        }

        for (
            int i = 0;
            i < rideRanked.Count;
            i++)
        {
            rideRanked[i].RidePreferenceRank =
                i + 1;
        }

        foreach (
            RoutingDebugInfo candidate
            in debugCandidates)
        {
            currentSnapshot.Candidates.Add(
                candidate);
        }

        Rebuild();
    }

    private List<Journey> SelectRepresentativeJourneys(
    List<Journey> journeys)
    {
        List<Journey> nonDominated =
            journeys
                .Where(journey =>
                    !journeys.Any(
                        other =>
                            other != journey &&
                            other.TotalWalkingDistanceMeters <=
                                journey.TotalWalkingDistanceMeters &&
                            other.TotalRideDistanceMeters <=
                                journey.TotalRideDistanceMeters &&
                            (
                                other.TotalWalkingDistanceMeters <
                                    journey.TotalWalkingDistanceMeters ||
                                other.TotalRideDistanceMeters <
                                    journey.TotalRideDistanceMeters
                            )))
                .ToList();

        if (nonDominated.Count <= 5)
        {
            return nonDominated
                .OrderBy(
                    journey =>
                        journey.TotalWalkingDistanceMeters)
                .ToList();
        }

        List<Journey> ordered =
            nonDominated
                .OrderBy(
                    journey =>
                        journey.TotalWalkingDistanceMeters)
                .ToList();

        List<Journey> selected =
            new();

        AddJourneyIfMissing(
            selected,
            ordered.First());

        AddJourneyIfMissing(
            selected,
            ordered.Last());

        if (selected.Count < 5)
        {
            AddJourneyIfMissing(
                selected,
                ordered[ordered.Count / 2]);
        }

        if (selected.Count < 5)
        {
            AddJourneyIfMissing(
                selected,
                ordered[ordered.Count / 4]);
        }

        if (selected.Count < 5)
        {
            AddJourneyIfMissing(
                selected,
                ordered[
                    (ordered.Count * 3) / 4]);
        }

        if (selected.Count < 5)
        {
            foreach (
                Journey journey
                in ordered)
            {
                AddJourneyIfMissing(
                    selected,
                    journey);

                if (selected.Count == 5)
                {
                    break;
                }
            }
        }

        return selected
            .OrderBy(
                journey =>
                    journey.TotalWalkingDistanceMeters)
            .ToList();
    }

    private void AddJourneyIfMissing(
    List<Journey> selected,
    Journey journey)
    {
        if (!selected.Contains(journey))
        {
            selected.Add(journey);
        }
    }
    private RoutingDebugInfo CreateDebugInfo(
        Journey journey,
        double maximumWalkingDistanceMeters)
    {
        RoutingDebugInfo debugInfo =
            new()
            {
                JourneyId =
                    BuildJourneyId(journey),

                FromWalkingDistance =
                    journey.Legs.Count > 0
                        ? journey.Legs.First()
                            .FromWalkingDistanceMeters
                        : 0,

                ToWalkingDistance =
                    journey.Legs.Count > 0
                        ? journey.Legs.Last()
                            .ToWalkingDistanceMeters
                        : 0,

                TotalWalkingDistance =
                    journey.TotalWalkingDistanceMeters,

                RideDistanceMeters =
                    journey.TotalRideDistanceMeters,

                NumberOfRides =
                    journey.NumberOfRides,

                NumberOfTransfers =
                    journey.NumberOfTransfers,

                Viable =
                    journey.TotalWalkingDistanceMeters <=
                    maximumWalkingDistanceMeters,

                Score =
                    0
            };

        foreach (
            JourneyLeg leg
            in journey.Legs)
        {
            debugInfo.Legs.Add(
                new RoutingDebugLegInfo
                {
                    RouteId =
                        leg.RouteId,

                    RouteName =
                        leg.RouteName,

                    DirectionName =
                        leg.DirectionName,

                    FromWalkingDistance =
                        leg.FromWalkingDistanceMeters,

                    ToWalkingDistance =
                        leg.ToWalkingDistanceMeters,

                    RideDistanceMeters =
                        leg.RideDistanceMeters
                });
        }

        return debugInfo;
    }

    private string BuildJourneyId(
        Journey journey)
    {
        return string.Join(
            "→",
            journey.Legs.Select(
                leg =>
                    $"{leg.RouteId}:{leg.DirectionName}"));
    }

    private void Rebuild()
    {
        contentLayout.Children.Clear();

        if (currentSnapshot == null)
        {
            ShowMessage(
                "Set a From and To location.");

            return;
        }

        AddHeader();

        if (currentSnapshot.Candidates.Count == 0)
        {
            ShowMessage(
                "No viable routes found.");

            return;
        }

        AddSortControls();

        List<RoutingDebugInfo> candidates =
            GetSortedCandidates();

        foreach (
            RoutingDebugInfo candidate
            in candidates)
        {
            RoutingCandidateCard card =
                new(candidate);

            card.Selected +=
                CandidateCard_Selected;

            contentLayout.Children.Add(
                card);
        }
    }

    private void AddHeader()
    {
        contentLayout.Children.Add(
            new Label
            {
                Text =
                    $"Journeys " +
                    $"({currentSnapshot!.Candidates.Count})",

                FontSize = 20,

                FontAttributes =
                    FontAttributes.Bold
            });

        contentLayout.Children.Add(
            new Label
            {
                Text =
                    $"Walking preference: " +
                    $"{currentSnapshot.MaximumWalkingDistanceMeters:F0} m",

                FontSize = 13
            });
    }

    private void AddSortControls()
    {
        HorizontalStackLayout controls =
            new()
            {
                Spacing = 8
            };

        Button walkButton =
            new()
            {
                Text =
                    "Walk: least → most",

                FontSize = 12,

                HorizontalOptions =
                    LayoutOptions.Fill
            };

        Button rideButton =
            new()
            {
                Text =
                    "Ride: least → most",

                FontSize = 12,

                HorizontalOptions =
                    LayoutOptions.Fill
            };

        walkButton.Clicked +=
            (sender, args) =>
            {
                currentSortMode =
                    SortMode.Walk;

                Rebuild();
            };

        rideButton.Clicked +=
            (sender, args) =>
            {
                currentSortMode =
                    SortMode.Ride;

                Rebuild();
            };

        controls.Children.Add(
            walkButton);

        controls.Children.Add(
            rideButton);

        contentLayout.Children.Add(
            controls);
    }

    private List<RoutingDebugInfo>
        GetSortedCandidates()
    {
        IEnumerable<RoutingDebugInfo> candidates =
            currentSnapshot!.Candidates;

        if (
            currentSortMode ==
            SortMode.Ride)
        {
            return candidates
                .OrderBy(
                    candidate =>
                        candidate.RideDistanceMeters)
                .ToList();
        }

        return candidates
            .OrderBy(
                candidate =>
                    candidate.TotalWalkingDistance)
            .ToList();
    }

    private void CandidateCard_Selected(
        object? sender,
        EventArgs e)
    {
        if (
            sender is not RoutingCandidateCard card)
        {
            return;
        }

        CandidateSelected?.Invoke(
            this,
            card.Candidate);
    }

    private void ShowMessage(
        string text)
    {
        contentLayout.Children.Add(
            new Label
            {
                Text = text,

                FontSize = 14
            });
    }
}