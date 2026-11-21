using Microsoft.Maui.Controls.Shapes;
using LugarLang.Mobile.Services.RoutingVisualization;
using LugarLang.Internal.Models;

namespace LugarLang.Mobile.UI.Routing;

public class JourneyDetailView : ContentView
{
    private const double MinimumTargetSize = 48;

    private static readonly Color WalkColor = Theme.Walk;
    private static readonly Color StartColor = Theme.StartPin;
    private static readonly Color SecondaryTextColor = Theme.TextSecondary;

    private readonly Label minutesLabel;
    private readonly Label summaryLabel;
    private readonly VerticalStackLayout timelineLayout;

    public event EventHandler? BackRequested;
    public event EventHandler<Place>? LandmarkDetailsRequested;
    public event EventHandler<Place>? PlaceSelected;
    public event EventHandler? SakayKoAniRequested;

    private Action? closeOpenLandmark;


    public JourneyDetailView()
    {
        Button backButton =
            new()
            {
                Text = "←",
                FontSize = 20,
                Padding = 0,
                WidthRequest = MinimumTargetSize,
                HeightRequest = MinimumTargetSize,
                CornerRadius = (int)(MinimumTargetSize / 2),
                BackgroundColor = Theme.Secondary,
                TextColor = Theme.Deep,
                VerticalOptions = LayoutOptions.Center
            };

        SemanticProperties.SetDescription(
            backButton,
            "Back to route options");

        backButton.Clicked +=
            (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

        minutesLabel =
            new Label
            {
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            };

        summaryLabel =
            new Label
            {
                FontSize = 13,
                TextColor = SecondaryTextColor,
                VerticalOptions = LayoutOptions.Center
            };

        HorizontalStackLayout header =
            new()
            {
                Spacing = 10,
                Padding = new Thickness(12, 8),
                Children =
                {
                    backButton,
                    minutesLabel,
                    summaryLabel
                }
            };

        timelineLayout =
            new VerticalStackLayout
            {
                Padding = new Thickness(14, 12)
            };

        ScrollView scroll =
            new()
            {
                Content = timelineLayout
            };

        Button sakayButton =
    new()
    {
        Text = "Sakay ko ani",
        FontSize = 16,
        FontAttributes = FontAttributes.Bold,
        HeightRequest = MinimumTargetSize,
        CornerRadius = (int)(MinimumTargetSize / 2),
        Margin = new Thickness(14, 8),
        BackgroundColor = Theme.Deep,
        TextColor = Colors.White
    };

        sakayButton.Clicked +=
            (_, _) => SakayKoAniRequested?.Invoke(this, EventArgs.Empty);

        Grid root =
            new()
            {
                BackgroundColor = Theme.PageBackground,
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };

        Grid.SetRow(header, 0);
        Grid.SetRow(scroll, 1);
        Grid.SetRow(sakayButton, 2);
        root.Children.Add(sakayButton);

        root.Children.Add(header);
        root.Children.Add(scroll);

        Content = root;
    }

    public void Show(
        RoutingDebugInfo candidate,
        List<List<Place>> landmarksPerLeg,
        Place? startPlace,
        Place? endPlace)
    {
        int minutes =
            Math.Max(1, (int)Math.Round(candidate.EstimatedMinutes));

        minutesLabel.Text = $"~{minutes} min";

        string rides =
            candidate.NumberOfRides == 1
                ? "Direct"
                : $"{candidate.NumberOfRides} rides";

        summaryLabel.Text =
            $"{rides} · walk {JourneyPresentation.FormatDistance(candidate.TotalWalkingDistance)}";

        timelineLayout.Children.Clear();
        closeOpenLandmark = null;

        timelineLayout.Children.Add(
    CreateRailRow(
        StartColor,
        WalkColor,
        BuildPlaceContent(startPlace, "Start"),
        isLast: false,
        dotSize: 14,
        dotTop: 17,
        lineTop: 24));

        for (int i = 0; i < candidate.Legs.Count; i++)
        {
            RoutingDebugLegInfo leg = candidate.Legs[i];

            string walkText =
                i == 0
                    ? $"Walk {JourneyPresentation.FormatDistance(leg.FromWalkingDistance)} to the route"
                    : leg.FromWalkingDistance < 30
                        ? "Change vehicles right here"
                        : $"Walk {JourneyPresentation.FormatDistance(leg.FromWalkingDistance)} to the next route";

            timelineLayout.Children.Add(
                CreateRailRow(
                    WalkColor,
                    WalkColor,
                    CreateTextBlock(walkText, null, false),
                    isLast: false));

            (int r, int g, int b) = JourneyPresentation.GetRideRgb(i);
            Color rideColor = Color.FromRgb(r, g, b);

            string direction =
                string.IsNullOrWhiteSpace(leg.DirectionSummary)
                    ? leg.DirectionName
                    : leg.DirectionSummary;

            timelineLayout.Children.Add(
                CreateRailRow(
                    rideColor,
                    rideColor,
                    CreateTextBlock(
                        $"Ride {leg.FullRouteName}",
                        $"{direction} · {JourneyPresentation.FormatKilometers(leg.RideDistanceMeters)}",
                        true),
                    isLast: false));

            foreach (Place landmark in landmarksPerLeg[i])
            {
                timelineLayout.Children.Add(
                    CreateRailRow(
                        rideColor,
                        rideColor,
                        BuildLandmarkContent(landmark),
                        isLast: false,
                        dotSize: 10,
                        hollow: true,
                        dotTop: 19,
                        lineTop: 0));
            }

            if (i == candidate.Legs.Count - 1)
            {
                timelineLayout.Children.Add(
                    CreateRailRow(
                        WalkColor,
                        WalkColor,
                        CreateTextBlock(
                            $"Walk {JourneyPresentation.FormatDistance(leg.ToWalkingDistance)} to your destination",
                            null,
                            false),
                        isLast: false));

                timelineLayout.Children.Add(
                    CreateRailRow(
                        Theme.EndPin,
                        WalkColor,
                        BuildPlaceContent(endPlace, "End"),
                        isLast: false,
                        dotSize: 14,
                        dotTop: 17,
                        lineTop: 0,
                        lineHeight: 24));
            }
        }
    }

    private static View CreateTextBlock(
        string title,
        string? subtitle,
        bool bold)
    {
        VerticalStackLayout block =
            new()
            {
                Spacing = 1,
                Padding = new Thickness(0, 0, 0, 14)
            };

        block.Children.Add(
            new Label
            {
                Text = title,
                FontSize = 14,
                TextColor = Theme.TextPrimary,
                FontAttributes =
                    bold ? FontAttributes.Bold : FontAttributes.None
            });

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            block.Children.Add(
                new Label
                {
                    Text = subtitle,
                    FontSize = 12,
                    TextColor = SecondaryTextColor
                });
        }

        return block;
    }

    // One timeline row: a dot and an unbroken line on the left, content
    // on the right. Landmark rows can reuse this later with a smaller dot.
    private static View CreateRailRow(
    Color dotColor,
    Color lineColor,
    View content,
    bool isLast,
    double dotSize = 12,
    bool hollow = false,
    double dotTop = 3,
    double lineTop = 9,
    double? lineHeight = null)
    {
        Border dot =
            new()
            {
                WidthRequest = dotSize,
                HeightRequest = dotSize,
                StrokeThickness = hollow ? 2.5 : 0,
                Stroke = new SolidColorBrush(dotColor),
                BackgroundColor = hollow ? Theme.PageBackground : dotColor,
                StrokeShape = new RoundRectangle { CornerRadius = dotSize / 2 },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, dotTop, 0, 0)
            };

        Grid rail = new() { WidthRequest = 14 };

        if (!isLast)
        {
            BoxView line =
                new()
                {
                    Color = lineColor,
                    WidthRequest = 3,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions =
                        lineHeight == null
                            ? LayoutOptions.Fill
                            : LayoutOptions.Start,
                    Margin = new Thickness(0, lineTop, 0, 0)
                };

            if (lineHeight != null)
                line.HeightRequest = lineHeight.Value;

            rail.Children.Add(line);
        }

        rail.Children.Add(dot);

        Grid row =
            new()
            {
                ColumnSpacing = 12,
                ColumnDefinitions =
                {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
                }
            };

        Grid.SetColumn(content, 1);

        row.Children.Add(rail);
        row.Children.Add(content);

        return row;
    }

    // One landmark row: the name (tap to open) and a panel with photos.
    private View BuildLandmarkContent(Place place, string? caption = null)
    {
        Label chevron =
            new()
            {
                Text = "▾",
                FontSize = 16,
                TextColor = Theme.TextSecondary,
                VerticalOptions = LayoutOptions.Center
            };

        VerticalStackLayout nameBlock =
     new()
     {
         Spacing = 0,
         VerticalOptions = LayoutOptions.Center
     };

        if (caption != null)
        {
            nameBlock.Children.Add(
                new Label
                {
                    Text = caption,
                    FontSize = 12,
                    TextColor = SecondaryTextColor
                });
        }

        nameBlock.Children.Add(
            new Label
            {
                Text = place.Name,
                TextColor = Theme.TextPrimary,
                FontSize = 14,
                FontAttributes =
                    caption != null ? FontAttributes.Bold : FontAttributes.None
            });

        Grid header =
            new()
            {
                MinimumHeightRequest = MinimumTargetSize,
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
                }
            };

        Grid.SetColumn(chevron, 1);
        header.Children.Add(nameBlock);
        header.Children.Add(chevron);

        // ---- photo area (tap to cycle, like PlaceDetailsPage) ----
        int photoIndex = 0;

        Image photo =
            new()
            {
                Aspect = Aspect.AspectFill,
                HeightRequest = 150,
                BackgroundColor = Theme.Placeholder
            };

        Label counter =
            new()
            {
                FontSize = 12,
                TextColor = Colors.White
            };

        Border counterBadge =
            new()
            {
                Padding = new Thickness(8, 2),
                Margin = 8,
                StrokeThickness = 0,
                BackgroundColor = Theme.Overlay,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Content = counter
            };

        Grid photoHost = new();
        photoHost.Children.Add(photo);
        photoHost.Children.Add(counterBadge);

        Grid nameStrip =
    new()
    {
        BackgroundColor = Theme.Overlay,
        VerticalOptions = LayoutOptions.End,
        HeightRequest = 44,
        Padding = new Thickness(2, 8),
        Children =
        {
            new Label
            {
                Text = place.Name,
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            }
        }
    };

        photoHost.Children.Add(nameStrip);

        void ShowPhoto()
        {
            if (place.Photos.Count == 0)
            {
                photoHost.IsVisible = false;
                return;
            }

            photo.Source = ImageSource.FromFile(place.Photos[photoIndex]);
            counter.Text = $"{photoIndex + 1} / {place.Photos.Count}";
            counterBadge.IsVisible = place.Photos.Count > 1;
        }

        TapGestureRecognizer photoTap = new();
        photoTap.Tapped +=
            (_, _) =>
            {
                if (place.Photos.Count < 2)
                    return;

                photoIndex = (photoIndex + 1) % place.Photos.Count;
                ShowPhoto();
            };
        photoHost.GestureRecognizers.Add(photoTap);

        ShowPhoto();

        // ---- details under the photo ----
        VerticalStackLayout body =
            new()
            {
                Spacing = 4,
                Padding = new Thickness(12, 10)
            };

        if (!string.IsNullOrWhiteSpace(place.Category))
        {
            body.Children.Add(
                new Label
                {
                    Text = place.Category,
                    FontSize = 12,
                    TextColor = SecondaryTextColor
                });
        }

        if (!string.IsNullOrWhiteSpace(place.Description))
        {
            body.Children.Add(
                new Label
                {
                    Text = place.Description,
                    TextColor = Theme.TextPrimary,
                    FontSize = 13,
                    MaxLines = 4,
                    LineBreakMode = LineBreakMode.TailTruncation
                });
        }

        Button detailsButton =
            new()
            {
                Text = "Details",
                FontSize = 13,
                HeightRequest = 44,
                Margin = new Thickness(0, 6, 0, 0),
                BackgroundColor = Theme.Secondary,
                TextColor = Theme.Deep,
                BorderColor = Theme.Border,
                BorderWidth = 1,
                CornerRadius = 14,
            };

        detailsButton.Clicked +=
            (_, _) => LandmarkDetailsRequested?.Invoke(this, place);

        body.Children.Add(detailsButton);

        VerticalStackLayout panelContent = new();

        Border photoFrame =
            new()
            {
                Padding = 0,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Content = photoHost
            };

        panelContent.Children.Add(photoFrame);

        panelContent.Children.Add(body);

        Border panel =
            new()
            {
                IsVisible = false,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = 0,
                StrokeThickness = 0,
                Content = panelContent
            };

        // Only one landmark open at a time.
        TapGestureRecognizer toggle = new();
        toggle.Tapped +=
    (_, _) =>
    {
        if (panel.IsVisible)
        {
            panel.IsVisible = false;
            chevron.Text = "▾";
            closeOpenLandmark = null;
            return;
        }

        closeOpenLandmark?.Invoke();

        panel.IsVisible = true;
        chevron.Text = "▴";

        closeOpenLandmark =
            () =>
            {
                panel.IsVisible = false;
                chevron.Text = "▾";
            };

        PlaceSelected?.Invoke(this, place);
    };
        header.GestureRecognizers.Add(toggle);

        VerticalStackLayout content = new();
        content.Children.Add(header);
        content.Children.Add(panel);

        return content;
    }

    // The start/end row: a real place opens like a landmark; a dropped
    // pin is just a label, since there is nothing to show for it.
    private View BuildPlaceContent(Place? place, string caption)
    {
        if (place != null)
            return BuildLandmarkContent(place, caption);

        Grid box = new() { MinimumHeightRequest = MinimumTargetSize };

        box.Children.Add(
            new VerticalStackLayout
            {
                Spacing = 0,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                new Label
                {
                    Text = caption,
                    FontSize = 12,
                    TextColor = SecondaryTextColor
                },
                new Label
                {
                    Text = "Dropped pin",
                    TextColor = Theme.TextPrimary,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold
                }
                }
            });

        return box;
    }
}