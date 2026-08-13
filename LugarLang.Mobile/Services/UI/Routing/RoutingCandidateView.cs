using Microsoft.Maui.Controls;
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

    public void Display(
        RoutingDebugSnapshot snapshot)
    {
        currentSnapshot = snapshot;

        Rebuild();
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
                    $"Routes " +
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

        if (currentSortMode ==
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
        if (sender is not RoutingCandidateCard card)
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
