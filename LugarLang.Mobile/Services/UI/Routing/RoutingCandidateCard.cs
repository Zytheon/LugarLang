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
        VerticalStackLayout layout = new() { Spacing = 6 };

        int minutes = Math.Max(1, (int)Math.Round(Candidate.EstimatedMinutes));

        string ridesText =
            Candidate.NumberOfRides == 1
                ? "Direct"
                : $"{Candidate.NumberOfRides} rides";

        layout.Children.Add(
            new Label
            {
                Text =
                    $"~{minutes} min · {ridesText} · " +
                    $"{JourneyPresentation.FormatDistance(Candidate.TotalWalkingDistance)} walk · " +
                    $"{JourneyPresentation.FormatKilometers(Candidate.RideDistanceMeters)} ride",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            });

        if (Candidate.Badges.Count > 0)
        {
            HorizontalStackLayout badgeRow = new() { Spacing = 6 };

            foreach (string badge in Candidate.Badges)
            {
                badgeRow.Children.Add(
                    new Border
                    {
                        Padding = new Thickness(8, 2),
                        StrokeThickness = 0,
                        BackgroundColor = Color.FromArgb("#DCEAFE"),
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Content = new Label
                        {
                            Text = badge,
                            FontSize = 12,
                            TextColor = Color.FromArgb("#174EA6")
                        }
                    });
            }

            layout.Children.Add(badgeRow);
        }

        if (!Candidate.Viable)
        {
            layout.Children.Add(
                new Label
                {
                    Text = "Total walking is more than your walking limit",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#B45309")
                });
        }

        for (int i = 0; i < Candidate.Legs.Count; i++)
        {
            RoutingDebugLegInfo leg = Candidate.Legs[i];

            string walkText =
                i == 0
                    ? $"Walk {JourneyPresentation.FormatDistance(leg.FromWalkingDistance)} to the route"
                    : leg.FromWalkingDistance < 30
                        ? "Change vehicles right here"
                        : $"Walk {JourneyPresentation.FormatDistance(leg.FromWalkingDistance)} to the next route";

            layout.Children.Add(CreateStepLabel(walkText));

            (int r, int g, int b) = JourneyPresentation.GetRideRgb(i);

            Border numberBadge =
                new()
                {
                    WidthRequest = 22,
                    HeightRequest = 22,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromRgb(r, g, b),
                    StrokeShape = new RoundRectangle { CornerRadius = 11 },
                    Content = new Label
                    {
                        Text = (i + 1).ToString(),
                        TextColor = Colors.White,
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };

            Label rideLabel =
                new()
                {
                    Text =
                        $"{leg.RouteName} {leg.DirectionLabel} · " +
                        $"{JourneyPresentation.FormatKilometers(leg.RideDistanceMeters)}",
                    FontSize = 14,
                    VerticalOptions = LayoutOptions.Center
                };

            Grid rideRow =
                new()
                {
                    ColumnSpacing = 8,
                    ColumnDefinitions =
                    {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                    }
                };

            Grid.SetColumn(rideLabel, 1);
            rideRow.Children.Add(numberBadge);
            rideRow.Children.Add(rideLabel);

            layout.Children.Add(rideRow);

            if (i == Candidate.Legs.Count - 1)
            {
                layout.Children.Add(
                    CreateStepLabel(
                        $"Walk {JourneyPresentation.FormatDistance(leg.ToWalkingDistance)} to your destination"));
            }
        }

#if DEBUG
    layout.Children.Add(
        new Label
        {
            Text =
                $"[debug] walk rank #{Candidate.WalkPreferenceRank}, " +
                $"ride rank #{Candidate.RidePreferenceRank}, " +
                $"transfers {Candidate.NumberOfTransfers}",
            FontSize = 10,
            TextColor = Colors.Gray
        });
#endif

        if (Candidate.GroupMembers.Count > 0)
        {
            layout.Children.Add(CreateGroupExpansion());
        }

        return layout;
    }

    private static Label CreateStepLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontSize = 12,
            TextColor = Color.FromArgb("#5F6368")
        };
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
                        $"~{Math.Max(1, (int)Math.Round(member.EstimatedMinutes))} min, " +
                        $"walk {member.TotalWalkingDistance:F0} m, " +
                        $"ride {JourneyPresentation.FormatKilometers(member.RideDistanceMeters)}",

                    FontSize = 12,

                    TextColor = Color.FromArgb("#174EA6"),
                    MinimumHeightRequest = 48,
                    VerticalTextAlignment = TextAlignment.Center
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
                MinimumHeightRequest = 48,
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