using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using LugarLang.Mobile.Services.RoutingVisualization;

namespace LugarLang.Mobile.UI.Routing;

public class RoutingCandidateCard : Border
{
    public RoutingDebugInfo Candidate { get; }

    /// <summary>
    /// Fired when the card itself (the group's representative) is
    /// tapped -- unchanged from before grouping existed.
    /// </summary>
    public event EventHandler? Selected;

    /// <summary>
    /// Fired when the user taps one of the expanded alternates under
    /// "Show N similar options". Carries that specific member's
    /// RoutingDebugInfo so the caller can select IT instead of the
    /// representative.
    /// </summary>
    public event EventHandler<RoutingDebugInfo>? MemberSelected;

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

        if (Candidate.GroupMembers.Count > 0)
        {
            layout.Children.Add(
                CreateGroupExpansion());
        }

        return layout;
    }

    /// <summary>
    /// Builds the "Show N similar options" toggle plus the (initially
    /// hidden) list of alternates it reveals. Each alternate is its own
    /// tappable row -- tapping it fires MemberSelected, not Selected, so
    /// the caller knows to select that specific journey instead of the
    /// group's representative.
    ///
    /// NOTE: this relies on a MAUI Button's Clicked handling NOT
    /// bubbling up to the Border's own TapGestureRecognizer (which
    /// would otherwise also fire Selected on every expand/collapse
    /// tap). That's normal MAUI behavior, but wasn't verified against
    /// a running build -- if tapping "Show N similar options" also
    /// triggers the card's main Selected event, that's the first thing
    /// to check.
    /// </summary>
    private View CreateGroupExpansion()
    {
        VerticalStackLayout membersLayout =
            new()
            {
                Spacing = 6,
                Margin = new Thickness(8, 4, 0, 0),
                IsVisible = false
            };

        foreach (
            RoutingDebugInfo member
            in Candidate.GroupMembers)
        {
            Label memberLabel =
                new()
                {
                    Text =
                        $"• {member.RouteSummary} — " +
                        $"walk {member.TotalWalkingDistance:F0}m, " +
                        $"ride {member.RideDistanceMeters:F0}m",

                    FontSize = 12,

                    TextColor =
                        Colors.CornflowerBlue
                };

            TapGestureRecognizer memberTap =
                new();

            memberTap.Tapped +=
                (sender, args) =>
                {
                    MemberSelected?.Invoke(
                        this,
                        member);
                };

            memberLabel.GestureRecognizers.Add(
                memberTap);

            membersLayout.Children.Add(
                memberLabel);
        }

        Button expandButton =
            new()
            {
                Text =
                    $"Show {Candidate.GroupMembers.Count} " +
                    $"similar option(s)",

                FontSize = 12
            };

        expandButton.Clicked +=
            (sender, args) =>
            {
                membersLayout.IsVisible =
                    !membersLayout.IsVisible;

                expandButton.Text =
                    membersLayout.IsVisible
                        ? "Hide similar options"
                        : $"Show {Candidate.GroupMembers.Count} " +
                          $"similar option(s)";
            };

        VerticalStackLayout wrapper =
            new()
            {
                Spacing = 4
            };

        wrapper.Children.Add(
            expandButton);

        wrapper.Children.Add(
            membersLayout);

        return wrapper;
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