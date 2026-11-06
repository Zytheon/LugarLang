
using LugarLang.Internal.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;

namespace LugarLang.Mobile.Pages;

public class ChooseRegionPage : ContentPage
{
    private readonly List<Place> places;
    private readonly Action<LocationPath> onPicked;
    private readonly Func<LocationPath, string?>? photoFor;

    private readonly VerticalStackLayout layout = new()
    {
        Spacing = 20
    };

    private Grid cardsGrid = new();

    private LocationPath browsing = LocationPath.None;

    // ---------------------------------------------------------
    // COLOR PALETTE
    // ---------------------------------------------------------

    private static readonly Color PageBackground =
        Color.FromArgb("#F1F6FA");

    private static readonly Color SurfaceWhite =
        Color.FromArgb("#FFFFFF");

    private static readonly Color PrimaryBlue =
        Color.FromArgb("#486F91");

    private static readonly Color DeepBlue =
        Color.FromArgb("#34566F");

    private static readonly Color SecondaryBlue =
        Color.FromArgb("#E5EEF6");

    private static readonly Color BorderBlue =
        Color.FromArgb("#D6E2ED");

    private static readonly Color TextPrimary =
        Color.FromArgb("#253B50");

    private static readonly Color TextSecondary =
        Color.FromArgb("#627B92");

    private static readonly Color CardPlaceholder =
        Color.FromArgb("#DCE6EE");

    private static readonly Color CardOverlay =
        Color.FromArgb("#CC203746");

    // ---------------------------------------------------------
    // RESPONSIVE DIMENSIONS
    // ---------------------------------------------------------

    private bool IsPhone =>
        DeviceInfo.Idiom == DeviceIdiom.Phone;

    private double CardWidth =>
        IsPhone ? 160 : 420;

    private double CardHeight =>
        IsPhone ? 120 : 260;

    private double PanelMargin =>
        IsPhone ? 8 : 24;

    private double GridPadding =>
        IsPhone ? 16 : 28;

    private double GridSpacing =>
        IsPhone ? 12 : 24;

    private double PageHeadingSize =>
        IsPhone ? 27 : 40;

    private double CardTitleSize =>
        IsPhone ? 16 : 25;

    public ChooseRegionPage(
        IEnumerable<Place> places,
        Action<LocationPath> onPicked,
        Func<LocationPath, string?>? photoFor = null)
    {
        this.places = places.ToList();
        this.onPicked = onPicked;
        this.photoFor = photoFor;

        Title = "Choose a Region";
        BackgroundColor = PageBackground;

        Grid root = new()
        {
            BackgroundColor = PageBackground
        };

        Border panel = new()
        {
            BackgroundColor = SurfaceWhite,
            Stroke = BorderBlue,
            StrokeThickness = 1,

            StrokeShape = new RoundRectangle
            {
                CornerRadius = 24
            },

            Padding = 0,
            Margin = PanelMargin,

            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,

            Content = layout
        };

        ScrollView scroll = new()
        {
            Content = panel,
            Orientation = ScrollOrientation.Vertical,

            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        root.Children.Add(scroll);

        Content = root;

        Render();
    }

    // ---------------------------------------------------------
    // CARD GRID
    // ---------------------------------------------------------

    private Grid CreateCardsGrid()
    {
        Grid grid = new()
        {
            ColumnSpacing = GridSpacing,
            RowSpacing = GridSpacing,

            Padding = GridPadding,

            HorizontalOptions = LayoutOptions.Center
        };

        // Maintain the two-column destination layout.
        for (int i = 0; i < 2; i++)
        {
            grid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = CardWidth
                });
        }

        return grid;
    }

    // ---------------------------------------------------------
    // MAIN PAGE RENDERING
    // ---------------------------------------------------------

    private void Render()
    {
        layout.Children.Clear();

        cardsGrid = CreateCardsGrid();

        string level = browsing.Depth switch
        {
            0 => "Region",
            1 => "Province",
            _ => "City"
        };

        // -----------------------------------------------------
        // HEADER
        // -----------------------------------------------------

        Label locationIcon = new()
        {
            Text = "⌖",
            FontSize = IsPhone ? 34 : 48,

            TextColor = Colors.White,

            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        Label heading = new()
        {
            Text = $"Choose a {level}",

            FontSize = PageHeadingSize,
            FontAttributes = FontAttributes.Bold,

            TextColor = Colors.White,

            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,

            LineBreakMode = LineBreakMode.WordWrap
        };

        VerticalStackLayout headerContent = new()
        {
            Spacing = 4,

            Padding = new Thickness(
                20,
                IsPhone ? 22 : 30,
                20,
                IsPhone ? 24 : 34),

            HorizontalOptions = LayoutOptions.Fill,

            Children =
            {
                locationIcon,
                heading
            }
        };

        Border header = new()
        {
            Content = headerContent,

            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),

                GradientStops =
                {
                    new GradientStop(
                        Color.FromArgb("#6689A5"), 0.0f),

                    new GradientStop(
                        Color.FromArgb("#486F91"), 0.5f),

                    new GradientStop(
                        Color.FromArgb("#294E68"), 1.0f)
                }
            },

            StrokeThickness = 0,

            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(
                    23, 23, 18, 18)
            },

            Margin = 0,
            Padding = 0,

            HorizontalOptions = LayoutOptions.Fill
        };

        layout.Children.Add(header);

        // -----------------------------------------------------
        // BREADCRUMB
        // -----------------------------------------------------

        if (!browsing.IsEmpty)
        {
            string breadcrumbText = string.Join(
                "   ›   ",
                new[]
                {
                    browsing.Region,
                    browsing.Province,
                    browsing.City
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

            Label breadcrumb = new()
            {
                Text = breadcrumbText,

                FontSize = IsPhone ? 13 : 17,

                TextColor = TextSecondary,

                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,

                Margin = new Thickness(12, 8, 12, 0),

                LineBreakMode = LineBreakMode.WordWrap
            };

            layout.Children.Add(breadcrumb);

            // -------------------------------------------------
            // NAVIGATION BUTTONS
            // -------------------------------------------------

            Button backButton = new()
            {
                Text = "‹  Back",

                FontSize = IsPhone ? 14 : 17,
                FontAttributes = FontAttributes.Bold,

                TextColor = DeepBlue,
                BackgroundColor = SecondaryBlue,

                CornerRadius = 24,
                HeightRequest = IsPhone ? 46 : 58,
                WidthRequest = IsPhone ? 100 : 150,

                Padding = new Thickness(12, 0),

                HorizontalOptions = LayoutOptions.Center
            };

            backButton.Clicked += (s, e) =>
            {
                browsing = browsing.Parent;
                Render();
            };

            Button dashboardButton = new()
            {
                Text = "⌂  Return to Dashboard",

                FontSize = IsPhone ? 13 : 17,
                FontAttributes = FontAttributes.Bold,

                TextColor = Colors.White,
                BackgroundColor = PrimaryBlue,

                CornerRadius = 28,
                HeightRequest = IsPhone ? 46 : 58,

                Padding = new Thickness(
                    IsPhone ? 12 : 24, 0),

                HorizontalOptions = LayoutOptions.Center
            };

            dashboardButton.Clicked += async (s, e) =>
                await Pick(browsing);

            Grid navigation = new()
            {
                ColumnSpacing = IsPhone ? 8 : 16,

                Padding = new Thickness(
                    IsPhone ? 12 : 32,
                    4,
                    IsPhone ? 12 : 32,
                    8),

                HorizontalOptions = LayoutOptions.Fill
            };

            navigation.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            navigation.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Star
                });

            Grid.SetColumn(backButton, 0);
            Grid.SetColumn(dashboardButton, 1);

            dashboardButton.HorizontalOptions =
                LayoutOptions.End;

            navigation.Children.Add(backButton);
            navigation.Children.Add(dashboardButton);

            layout.Children.Add(navigation);
        }

        // -----------------------------------------------------
        // SEPARATOR
        // -----------------------------------------------------

        BoxView separator = new()
        {
            HeightRequest = 1,

            BackgroundColor = BorderBlue,

            HorizontalOptions = LayoutOptions.Fill,

            Margin = new Thickness(
                IsPhone ? 16 : 32,
                0,
                IsPhone ? 16 : 32,
                0)
        };

        layout.Children.Add(separator);

        // -----------------------------------------------------
        // DESTINATION CARDS
        // -----------------------------------------------------

        List<(string Name, int Count)> children =
            LocationTree.Children(places, browsing);

        for (int i = 0; i < children.Count; i++)
        {
            string name = children[i].Name;

            LocationPath child = ChildPath(name);

            bool hasChildren =
                LocationTree.Children(places, child).Count > 0;

            View card = CreateChoiceCard(
                name,
                hasChildren,
                photoFor?.Invoke(child),
                async () =>
                {
                    if (hasChildren)
                    {
                        browsing = child;
                        Render();
                    }
                    else
                    {
                        await Pick(child);
                    }
                });

            int row = i / 2;
            int column = i % 2;

            while (cardsGrid.RowDefinitions.Count <= row)
            {
                cardsGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = CardHeight
                    });
            }

            Grid.SetRow(card, row);
            Grid.SetColumn(card, column);

            cardsGrid.Children.Add(card);
        }

        if (children.Count > 0)
        {
            layout.Children.Add(cardsGrid);
        }
        else
        {
            // -------------------------------------------------
            // EMPTY STATE
            // -------------------------------------------------

            Border emptyState = new()
            {
                BackgroundColor = SecondaryBlue,

                StrokeThickness = 0,

                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 18
                },

                Padding = new Thickness(20, 24),

                Margin = new Thickness(
                    IsPhone ? 16 : 32,
                    12,
                    IsPhone ? 16 : 32,
                    24),

                Content = new VerticalStackLayout
                {
                    Spacing = 10,

                    HorizontalOptions = LayoutOptions.Center,

                    Children =
                    {
                        new Label
                        {
                            Text = "⌖",

                            FontSize = 34,
                            TextColor = PrimaryBlue,

                            HorizontalOptions =
                                LayoutOptions.Center
                        },

                        new Label
                        {
                            Text = browsing.IsEmpty
                                ? "No regions have content yet."
                                : "No locations available here yet.",

                            FontSize = IsPhone ? 14 : 18,

                            TextColor = DeepBlue,

                            HorizontalTextAlignment =
                                TextAlignment.Center,

                            HorizontalOptions =
                                LayoutOptions.Center
                        }
                    }
                }
            };

            layout.Children.Add(emptyState);
        }

        // Bottom breathing room.
        layout.Children.Add(new BoxView
        {
            HeightRequest = 8,
            Opacity = 0
        });
    }

    // ---------------------------------------------------------
    // DESTINATION CARD
    // ---------------------------------------------------------

    private View CreateChoiceCard(
        string name,
        bool hasChildren,
        string? photo,
        Func<Task> onTap)
    {
        Image image = new()
        {
            Aspect = Aspect.AspectFill,

            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,

            BackgroundColor = CardPlaceholder
        };

        if (!string.IsNullOrWhiteSpace(photo))
        {
            image.Source = ImageSource.FromFile(photo);
        }

        // The label strip remains readable over bright photographs.
        Grid nameStrip = new()
        {
            BackgroundColor = CardOverlay,

            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill,

            HeightRequest = IsPhone ? 44 : 62,

            Padding = new Thickness(
                IsPhone ? 10 : 18,
                0,
                IsPhone ? 8 : 14,
                0),

            ColumnSpacing = 6
        };

        nameStrip.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Star
            });

        nameStrip.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Auto
            });

        Label nameLabel = new()
        {
            Text = name,

            TextColor = Colors.White,

            FontSize = CardTitleSize,
            FontAttributes = FontAttributes.Bold,

            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,

            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        Grid.SetColumn(nameLabel, 0);
        nameStrip.Children.Add(nameLabel);

        if (hasChildren)
        {
            Label chevron = new()
            {
                Text = "›",

                FontSize = IsPhone ? 28 : 36,
                FontAttributes = FontAttributes.Bold,

                TextColor = Colors.White,

                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            Grid.SetColumn(chevron, 1);
            nameStrip.Children.Add(chevron);
        }

        Grid content = new()
        {
            BackgroundColor = CardPlaceholder
        };

        content.Children.Add(image);
        content.Children.Add(nameStrip);

        Border card = new()
        {
            Content = content,

            WidthRequest = CardWidth,
            HeightRequest = CardHeight,

            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,

            StrokeThickness = 0,

            StrokeShape = new RoundRectangle
            {
                CornerRadius = 17
            }
        };

        TapGestureRecognizer tap = new();

        tap.Tapped += async (s, e) =>
            await onTap();

        card.GestureRecognizers.Add(tap);

        SemanticProperties.SetDescription(
            card,
            hasChildren
                ? $"{name}. Open to choose a more specific location."
                : name);

        return card;
    }

    // ---------------------------------------------------------
    // LOCATION NAVIGATION
    // ---------------------------------------------------------

    private LocationPath ChildPath(string name) =>
        browsing.Depth switch
        {
            0 => LocationPath.Of(name),

            1 => LocationPath.Of(
                browsing.Region,
                name),

            _ => LocationPath.Of(
                browsing.Region,
                browsing.Province,
                name)
        };

    private async Task Pick(LocationPath path)
    {
        onPicked(path);

        await Navigation.PopAsync();
    }
}
