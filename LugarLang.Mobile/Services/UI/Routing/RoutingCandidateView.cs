using Microsoft.Maui.Controls;
using LugarLang.Mobile.Services.Routing;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.UI.Routing;

public class RoutingCandidateView : ContentView
{
    private readonly VerticalStackLayout contentLayout;

    private readonly JourneyGroupingService journeyGroupingService =
        new();

    /// <summary>
    /// Replaces the old "top 5 journeys" cap. Now caps GROUPS, not raw
    /// journeys -- since a group can absorb several near-duplicate
    /// routes into one row, 10 groups represents meaningfully more
    /// diversity than 10 (or even 5) ungrouped journeys did. Tune this
    /// once you've seen it against a few real queries.
    /// </summary>
    private const int MaximumGroupsShown = 10;

    private RoutingDebugSnapshot? currentSnapshot;

    private enum SortMode
    {
        Time,
        Walk,
        Ride
    }

    private SortMode currentSortMode =
        SortMode.Time;
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
        List<Journey> allJourneys =
            journeys.ToList();

        List<JourneyGroup> groups =
            journeyGroupingService.Group(
                allJourneys);

        List<JourneyGroup> topGroups =
            groups
                .OrderBy(
                    group =>
                        SegmentGraphJourneyRoutingService.EstimateTotalMinutes(
                            group.Representative))
                .Take(MaximumGroupsShown)
                .ToList();

        currentSnapshot =
            new RoutingDebugSnapshot
            {
                MaximumWalkingDistanceMeters =
                    maximumWalkingDistanceMeters
            };

        List<RoutingDebugInfo> debugCandidates =
            new();

        foreach (
            JourneyGroup group
            in topGroups)
        {
            RoutingDebugInfo representativeInfo =
                CreateDebugInfo(
                    group.Representative,
                    maximumWalkingDistanceMeters);

            List<Journey> otherMembers =
                group.Members
                    .Where(member => member != group.Representative)
                    .OrderBy(
                        member =>
                            SegmentGraphJourneyRoutingService.EstimateTotalMinutes(member))
                    .ToList();

            representativeInfo.GroupMembers =
                otherMembers
                    .Select(
                        member =>
                            CreateDebugInfo(
                                member,
                                maximumWalkingDistanceMeters))
                    .ToList();

            debugCandidates.Add(
                representativeInfo);
        }

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

        if (debugCandidates.Count > 1)
        {
            debugCandidates
                .OrderBy(candidate => candidate.EstimatedMinutes)
                .First()
                .Badges.Add("Fastest");

            debugCandidates
                .OrderBy(candidate => candidate.TotalWalkingDistance)
                .First()
                .Badges.Add("Least walking");
        }

        foreach (RoutingDebugInfo candidate in debugCandidates)
        {
            if (candidate.NumberOfRides == 1)
            {
                candidate.Badges.Add("Direct");
            }
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

            card.MemberSelected +=
                CandidateCard_MemberSelected;

            contentLayout.Children.Add(
                card);
        }
    }

    private void AddHeader()
    {
        int totalJourneys =
            currentSnapshot!.Candidates.Sum(
                candidate => 1 + candidate.GroupMembers.Count);

        contentLayout.Children.Add(
            new Label
            {
                Text = $"Route options ({totalJourneys})",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold
            });

        contentLayout.Children.Add(
            new Label
            {
                Text =
                    "Flag a jeepney anywhere along the colored line. " +
                    "Times are rough estimates (about 5 km/h walking, " +
                    "15 km/h riding, plus a few minutes per transfer). " +
                    $"Walking limit: {currentSnapshot.MaximumWalkingDistanceMeters:F0} m.",
                FontSize = 12,
                TextColor = Color.FromArgb("#5F6368")
            });
    }

    private void AddSortControls()
    {
        HorizontalStackLayout controls = new() { Spacing = 8 };

        controls.Children.Add(CreateSortButton("Fastest", SortMode.Time));
        controls.Children.Add(CreateSortButton("Least walking", SortMode.Walk));
        controls.Children.Add(CreateSortButton("Shortest ride", SortMode.Ride));

        contentLayout.Children.Add(controls);
    }

    private Button CreateSortButton(string text, SortMode mode)
    {
        bool selected = currentSortMode == mode;


        Button button =
            new()
            {
                Text = text,
                FontSize = 12,
                BackgroundColor =
                    selected ? Color.FromArgb("#DCEAFE") : Colors.Transparent,
                TextColor =
                    selected ? Color.FromArgb("#174EA6") : Colors.DarkSlateGray,
                MinimumHeightRequest = 48,
                Padding = new Thickness(12, 0),
            };

        button.Clicked +=
            (sender, args) =>
            {
                currentSortMode = mode;
                Rebuild();
            };

        return button;
    }

    private List<RoutingDebugInfo> GetSortedCandidates()
    {
        IEnumerable<RoutingDebugInfo> candidates =
            currentSnapshot!.Candidates;

        return currentSortMode switch
        {
            SortMode.Ride =>
                candidates.OrderBy(c => c.RideDistanceMeters).ToList(),
            SortMode.Walk =>
                candidates.OrderBy(c => c.TotalWalkingDistance).ToList(),
            _ =>
                candidates.OrderBy(c => c.EstimatedMinutes).ToList()
        };
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

    private void CandidateCard_MemberSelected(
        object? sender,
        RoutingDebugInfo member)
    {
        // Reuses the SAME event as a normal card tap. MapPage's handler
        // looks up the matching Journey by JourneyId regardless of
        // whether it came from a top-level card or an expanded group
        // member, so no changes were needed on that side.
        CandidateSelected?.Invoke(
            this,
            member);
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