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
                    Candidate.Legs.Count == 1
                        ? Candidate.Legs[0].RouteName
                        : "Journey",

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold
            });

        if (Candidate.Legs.Count > 0)
        {
            for (
                int i = 0;
                i < Candidate.Legs.Count;
                i++)
            {
                RoutingDebugLegInfo leg =
                    Candidate.Legs[i];

                layout.Children.Add(
                    new Label
                    {
                        Text =
                            $"Ride {i + 1}: " +
                            $"{leg.RouteName} " +
                            $"({leg.DirectionName})",

                        FontSize = 13
                    });

                layout.Children.Add(
                    new Label
                    {
                        Text =
                            $"  Walk before: " +
                            $"{leg.FromWalkingDistance:F0} m",

                        FontSize = 12
                    });

                layout.Children.Add(
                    new Label
                    {
                        Text =
                            $"  Ride: " +
                            $"{leg.RideDistanceMeters:F0} m",

                        FontSize = 12
                    });

                if (
                    i ==
                    Candidate.Legs.Count - 1)
                {
                    layout.Children.Add(
                        new Label
                        {
                            Text =
                                $"  Walk after: " +
                                $"{leg.ToWalkingDistance:F0} m",

                            FontSize = 12
                        });
                }
                else
                {
                    layout.Children.Add(
                        new Label
                        {
                            Text =
                                $"  Transfer walk: " +
                                $"{leg.ToWalkingDistance:F0} m",

                            FontSize = 12
                        });
                }
            }
        }

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
                    $"Total ride: " +
                    $"{Candidate.RideDistanceMeters:F0} m",

                FontSize = 13
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Rides: " +
                    $"{Candidate.NumberOfRides}",

                FontSize = 12
            });

        layout.Children.Add(
            new Label
            {
                Text =
                    $"Transfers: " +
                    $"{Candidate.NumberOfTransfers}",

                FontSize = 12
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