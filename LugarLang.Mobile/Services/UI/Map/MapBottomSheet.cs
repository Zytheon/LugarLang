using Microsoft.Maui.Controls.Shapes;
using LugarLang.Internal.Models;
using Microsoft.Maui.Devices;

namespace LugarLang.Mobile.UI.Map;

/// <summary>
/// Bottom sheet with two selectable panels (A = selected places, Map =
/// map and routes). The top bar (double chevron in the middle, panel
/// buttons on the right) is always visible: when the sheet is closed it
/// is the only part showing, as an overhang along the bottom edge.
/// Clicking or tapping the bar (or swiping it) opens or closes the sheet.
/// When open, the sheet height fits the active panel: panel A opens just
/// tall enough for its content, the map panel opens to a fixed share of
/// the page height. The position is always recomputed from the
/// open/closed state, so resizing the window can never leave it half open.
/// </summary>
public sealed class MapBottomSheet : ContentView
{
    // ---- Sizing -----------------------------------------------------

    private const double HeaderHeight = 58;
    private const double ClosedVisibleHeight = HeaderHeight + 1;
    private const double MinimumTargetSize = 48;
    private const double MaximumSheetWidth = 800;
    private const double MapPanelHeightRatio = 0.86;
    private const double PanelAContentHeight = 252;

    private const uint AnimationMs = 250;
    private const string HeightAnimationName = "SheetHeight";
    private const string SecondaryTextColorHex = "#5F6368";

    // ---- Controls ---------------------------------------------------

    private readonly Grid sheetLayout;
    private readonly Grid mapHost;
    private readonly Border sheetBorder;
    private readonly Button panelAButton;
    private readonly Button mapButton;
    private readonly Grid chevron;

    private Button findRoutesButton = null!;
    private MapPanelView? mapPanelView;
    private VisualElement? host;

    // ---- State ------------------------------------------------------

    private double pageHeight;
    private string selectedPanel = "A";
    private bool isOpen;
    private bool hasLayout;
    private double lastPanTotalY;
    private Place? shownFrom;
    private Place? shownTo;

    public MapBottomSheet()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.End;

        // Hidden until the first layout pass has put the sheet in its
        // closed position, so it never flashes open at startup.
        Opacity = 0;

        panelAButton = CreatePanelButton("A", "Selected places");
        mapButton = CreatePanelButton("🗺️", "Map and routes");

        panelAButton.Clicked += (_, _) => SelectPanel("A");
        mapButton.Clicked += (_, _) => SelectPanel("Map");

        HorizontalStackLayout panelButtons = new HorizontalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 4, 0),
            Children =
            {
                panelAButton,
                mapButton
            }
        };

        // Double chevron: points up when closed, down when open.
        chevron = CreateChevron();
        chevron.Rotation = 180;

        SemanticProperties.SetDescription(chevron, "Open or close the panel");

        // Top bar: [ empty | chevron (centered) | panel buttons ].
        Grid header = new Grid
        {
            HeightRequest = HeaderHeight,
            BackgroundColor = Colors.White,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            Padding = new Thickness(4, 0)
        };

        Grid.SetColumn(chevron, 1);
        Grid.SetColumn(panelButtons, 2);

        header.Children.Add(chevron);
        header.Children.Add(panelButtons);

        // The whole bar toggles the sheet, and swiping it opens or closes it.
        TapGestureRecognizer tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SetOpen(!isOpen);
        header.GestureRecognizers.Add(tap);

        PanGestureRecognizer pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        header.GestureRecognizers.Add(pan);

        mapHost = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        sheetLayout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        Grid.SetRow(header, 0);
        Grid.SetRow(mapHost, 1);

        sheetLayout.Children.Add(header);
        sheetLayout.Children.Add(mapHost);

        sheetBorder = new Border
        {
            Content = sheetLayout,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Padding = 0,
            BackgroundColor = Colors.White,
            Stroke = new SolidColorBrush(Colors.LightGray),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(18, 18, 0, 0)
            }
        };

        Content = sheetBorder;

        // Remember the endpoints that already exist at startup.
        shownFrom = RouteEndpoints.From;
        shownTo = RouteEndpoints.To;

        ShowPanelA();

        UpdatePanelButtonStyles();
    }

    // ---- Building blocks ---------------------------------------------

    private static Button CreatePanelButton(string text, string description)
    {
        Button button = new Button
        {
            Text = text,
            FontSize = 14,
            Padding = new Thickness(10, 0),
            HeightRequest = MinimumTargetSize,
            MinimumWidthRequest = MinimumTargetSize,
            CornerRadius = 10,
            BorderWidth = 0,
            VerticalOptions = LayoutOptions.Center
        };

        SemanticProperties.SetDescription(button, description);

        return button;
    }

    private static Grid CreateChevron()
    {
        Polyline CreateArrow(double top)
        {
            return new Polyline
            {
                Points = new PointCollection
                {
                    new Microsoft.Maui.Graphics.Point(2, 2),
                    new Microsoft.Maui.Graphics.Point(10, 8),
                    new Microsoft.Maui.Graphics.Point(18, 2)
                },
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 3,
                StrokeLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                WidthRequest = 20,
                HeightRequest = 10,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, top, 0, 0)
            };
        }

        return new Grid
        {
            WidthRequest = 24,
            HeightRequest = 22,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                CreateArrow(0),
                CreateArrow(8)
            }
        };
    }

    // ---- Panels --------------------------------------------------------

    private void SelectPanel(string panel)
    {
        selectedPanel = panel;

        switch (panel)
        {
            case "A":
                ShowPanelA();
                break;

            case "Map":
                ShowMapPreview();
                break;
        }

        UpdatePanelButtonStyles();

        SetOpen(true);
    }

    private void ShowMapPreview()
    {
        mapPanelView ??= new MapPanelView();

        mapHost.Children.Clear();
        mapHost.Children.Add(mapPanelView);
    }

    private void ShowPanelA()
    {
        mapHost.Children.Clear();

        bool ready =
            RouteEndpoints.From != null &&
            RouteEndpoints.To != null;

        findRoutesButton = new Button
        {
            Text = "Find Routes",
            HeightRequest = 50,
            CornerRadius = 12,
            TextColor = Colors.White,
            IsEnabled = ready,
            BackgroundColor = ready
                ? Color.FromArgb("#1A73E8")
                : Color.FromArgb("#C4C7CC")
        };

        findRoutesButton.Clicked += OnFindRoutesClicked;

        mapHost.Children.Add(
            new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    BuildEndpointRow("Start", RouteEndpoints.From),
                    BuildEndpointRow("End", RouteEndpoints.To),
                    findRoutesButton
                }
            });
    }

    private void OnFindRoutesClicked(object? sender, EventArgs e)
    {
        if (RouteEndpoints.From == null || RouteEndpoints.To == null)
            return;

        SelectPanel("Map");

        mapPanelView?.FindRoutes(
            RouteEndpoints.From,
            RouteEndpoints.To);
    }

    private static View BuildEndpointRow(string caption, Place? place)
    {
        Image photo = new Image
        {
            Aspect = Aspect.AspectFill,
            BackgroundColor = Color.FromArgb("#E5E7EB")
        };

        if (place != null && place.Photos.Count > 0)
            photo.Source = ImageSource.FromFile(place.Photos[0]);

        Border photoFrame = new Border
        {
            Content = photo,
            WidthRequest = 72,
            HeightRequest = 72,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };

        VerticalStackLayout text = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = caption,
                    FontSize = 12,
                    TextColor = Color.FromArgb(SecondaryTextColorHex)
                },
                new Label
                {
                    Text = place?.Name ?? "Not set",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    MaxLines = 2,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        };

        Grid row = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };

        Grid.SetColumn(text, 1);
        row.Children.Add(photoFrame);
        row.Children.Add(text);

        return row;
    }

    private void UpdatePanelButtonStyles()
    {
        Color selectedColor = Color.FromArgb("#DCEAFE");
        Color normalColor = Colors.Transparent;
        Color selectedTextColor = Color.FromArgb("#174EA6");
        Color normalTextColor = Colors.DarkSlateGray;

        panelAButton.BackgroundColor =
            selectedPanel == "A" ? selectedColor : normalColor;

        mapButton.BackgroundColor =
            selectedPanel == "Map" ? selectedColor : normalColor;

        panelAButton.TextColor =
            selectedPanel == "A" ? selectedTextColor : normalTextColor;

        mapButton.TextColor =
            selectedPanel == "Map" ? selectedTextColor : normalTextColor;
    }

    // Refresh the displayed endpoints whenever returning from place details.
    public void ApplyPendingLocationSelection()
    {
        if (ReferenceEquals(shownFrom, RouteEndpoints.From) &&
            ReferenceEquals(shownTo, RouteEndpoints.To))
        {
            return;
        }

        shownFrom = RouteEndpoints.From;
        shownTo = RouteEndpoints.To;

        // Rebuilds panel A with the new places and slides the sheet open.
        SelectPanel("A");
    }

    // ---- Host size and sheet position ---------------------------------

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (host != null)
            host.SizeChanged -= OnHostSizeChanged;

        host = Parent as VisualElement;

        if (host != null)
        {
            host.SizeChanged += OnHostSizeChanged;
            UpdatePageDimensions();
        }
    }

    private void OnHostSizeChanged(object? sender, EventArgs e)
    {
        UpdatePageDimensions();
    }

    private void UpdatePageDimensions()
    {
        double height = host?.Height ?? 0;

        if (height <= 0)
            return;

        pageHeight = height;

        double hostWidth = host?.Width ?? 0;

        // Phones: occupy the full available width.
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            HorizontalOptions = LayoutOptions.Fill;
            WidthRequest = -1;
        }
        // Tablets, laptops and desktop PCs: centered, maximum 900.
        else
        {
            if (hostWidth > MaximumSheetWidth)
            {
                HorizontalOptions = LayoutOptions.Center;
                WidthRequest = MaximumSheetWidth;
            }
            else
            {
                HorizontalOptions = LayoutOptions.Fill;
                WidthRequest = -1;
            }
        }

        ApplySheetState(animate: false);

        hasLayout = true;
        Opacity = 1;
    }

    private double GetOpenHeight()
    {
        double wanted =
            selectedPanel == "Map"
                ? pageHeight * MapPanelHeightRatio
                : HeaderHeight + PanelAContentHeight;

        return Math.Min(
            pageHeight,
            Math.Max(wanted, ClosedVisibleHeight + 1));
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        ApplySheetState(animate: hasLayout);

        _ = chevron.RotateTo(
            isOpen ? 0 : 180,
            AnimationMs,
            Easing.CubicOut);
    }

    private void ApplySheetState(bool animate)
    {
        if (pageHeight <= 0)
            return;

        double targetHeight = GetOpenHeight();
        double targetY = isOpen ? 0 : targetHeight - ClosedVisibleHeight;

        this.CancelAnimations();
        this.AbortAnimation(HeightAnimationName);

        double startHeight =
            HeightRequest > 0 ? HeightRequest : targetHeight;

        double startY = TranslationY;
        bool wasOpen = startY < 1;

        if (!animate)
        {
            HeightRequest = targetHeight;
            TranslationY = targetY;
            return;
        }

        if (wasOpen && isOpen && Math.Abs(startHeight - targetHeight) >= 1)
        {
            new Animation(
                value => HeightRequest = value,
                startHeight,
                targetHeight)
                .Commit(
                    this,
                    HeightAnimationName,
                    16,
                    AnimationMs,
                    Easing.CubicOut);

            return;
        }

        if (Math.Abs(startHeight - targetHeight) >= 1)
        {
            HeightRequest = targetHeight;
            TranslationY = startY + (targetHeight - startHeight);
        }

        _ = this.TranslateTo(0, targetY, AnimationMs, Easing.CubicOut);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!hasLayout)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                lastPanTotalY = e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                const double swipeThreshold = 40;

                if (lastPanTotalY <= -swipeThreshold)
                    SetOpen(true);
                else if (lastPanTotalY >= swipeThreshold)
                    SetOpen(false);

                lastPanTotalY = 0;
                break;
        }
    }
}