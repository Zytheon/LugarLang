using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.UI.Routing;

public class RoutingCandidateCard : Border
{
    public RoutingDebugInfo Candidate { get; }

    public event EventHandler? Selected;

    public RoutingCandidateCard(
        RoutingDebugInfo candidate)
    {
        Candidate = candidate;

        Padding = 12;

        StrokeThickness = 1;

        StrokeShape =
            new RoundRectangle
            {
                CornerRadius = 10
            };

        BackgroundColor =
            Colors.Transparent;

        Content =
            CreateContent();

        TapGestureRecognizer tapGesture =
            new();

        tapGesture.Tapped +=
            OnTapped;

        GestureRecognizers.Add(
            tapGesture);
    }

    private View CreateContent()
    {
        VerticalStackLayout layout =
            new()
            {
                Spacing = 4
            };

        layout.Children.Add(
            new Label
            {
                Text =
                    $"{Candidate.RouteName}",

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Direction: " +
                    $"{(Candidate.DirectionCorrect
                        ? "RIGHT"
                        : "WRONG")}",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"First walk: " +
                    $"{Candidate.FromWalkingDistance:F0} m",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Ride: " +
                    $"{Candidate.RideDistanceMeters:F0} m",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Second walk: " +
                    $"{Candidate.ToWalkingDistance:F0} m",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Total walk: " +
                    $"{Candidate.TotalWalkingDistance:F0} m",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Walk preference rank: " +
                    $"#{Candidate.WalkPreferenceRank}",

                FontSize = 12
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Ride preference rank: " +
                    $"#{Candidate.RidePreferenceRank}",

                FontSize = 12
            });

        return layout;
    }

    private void OnTapped(
        object? sender,
        TappedEventArgs e)
    {
        Selected?.Invoke(
            this,
            EventArgs.Empty);
    }
}
