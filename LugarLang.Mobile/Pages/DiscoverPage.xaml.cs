
using LugarLang.Internal.Controls.Developer;
using LugarLang.Internal.Models;
using LugarLang.Internal.Models.Discovery;
using LugarLang.Internal.Pages;
using LugarLang.Internal.Services;
using LugarLang.Internal.Services.Content;
using LugarLang.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls.Shapes;
using System.IO;
using System.Linq;

namespace LugarLang.Mobile.Pages;

public partial class DiscoverPage : ContentPage
{
    private readonly CategoryContentService categoryContentService;
    private readonly PlaceContentService placeContentService;
    private readonly ContentSyncService contentSyncService;
    private readonly PhotoCacheService photoCacheService;
    private readonly RegionMediaService regionMediaService;
    private readonly RideHailingProviderContentService
    rideHailingProviderContentService;

#if DEV
    private readonly bool isDeveloperMode = true;
#else
    private readonly bool isDeveloperMode = false;
#endif

    private string? selectedRegion;

    private const string SelectedRegionKey =
        "discover.selectedRegion";

    private const string DefaultRegion =
        "Northern Mindanao";

    private Place? selectedPlace;

    private bool isSwitchingPanel;

    private enum DiscoverPanel
    {
        Categories,
        RideHailing
    }

    private DiscoverPanel activePanel =
        DiscoverPanel.Categories;

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

    private const double CardWidth = 180;
    private const double CardHeight = 145;




    // ---------------------------------------------------------
    // CONSTRUCTOR
    // ---------------------------------------------------------

    public DiscoverPage()
    {
        InitializeComponent();

        System.Diagnostics.Debug.WriteLine($"APPDATA '{FileSystem.AppDataDirectory}'");

        // Retrieve the shared application services.
        var services =
            App.Current!.Handler!.MauiContext!.Services;

        categoryContentService =
            services.GetRequiredService<
                CategoryContentService>();

        placeContentService =
            services.GetRequiredService<
                PlaceContentService>();

        photoCacheService =
            services.GetRequiredService<
                PhotoCacheService>();

        rideHailingProviderContentService =
            services.GetRequiredService<
                RideHailingProviderContentService>();

        BackgroundColor = PageBackground;

        // Region media remains independently managed.
        regionMediaService = new RegionMediaService();

        // Keep the existing API configuration.
        contentSyncService = new ContentSyncService(
            new HttpClient
            {
                BaseAddress = new Uri(
                    LugarLangApiConfiguration.BaseUrl)
            },
            categoryContentService,
            placeContentService,
            photoCacheService);

        selectedRegion = LoadSavedRegion();

        UpdateBreadcrumbs();

        ExploreCategoriesPanel.IsVisible = true;
        ExploreCategoriesPanel.Opacity = 1;
        ExploreCategoriesPanel.TranslationX = 0;

        RideHailingPanel.IsVisible = false;
        RideHailingPanel.Opacity = 0;
        RideHailingPanel.TranslationX = 40;

        UpdateTabAppearance(DiscoverPanel.Categories);

        RefreshRideHailingProviders();
    }

    // ---------------------------------------------------------
    // PANEL SWITCHING
    // ---------------------------------------------------------

    private async void OnExploreTabClicked(
        object sender,
        EventArgs e)
    {
        await SwitchPanelAsync(DiscoverPanel.Categories);
    }

    private async void OnRideTabClicked(
        object sender,
        EventArgs e)
    {
        await SwitchPanelAsync(DiscoverPanel.RideHailing);
    }

    private async Task SwitchPanelAsync(
        DiscoverPanel targetPanel)
    {
        if (isSwitchingPanel || targetPanel == activePanel)
            return;

        isSwitchingPanel = true;

        try
        {
            bool movingToRight =
                targetPanel == DiscoverPanel.RideHailing;

            View outgoingPanel =
                activePanel == DiscoverPanel.Categories
                    ? ExploreCategoriesPanel
                    : RideHailingPanel;

            View incomingPanel =
                targetPanel == DiscoverPanel.Categories
                    ? ExploreCategoriesPanel
                    : RideHailingPanel;

            double slideDistance =
                Math.Max(
                    DiscoveryPanelsHost.Width * 0.18,
                    48);

            double outgoingOffset =
                movingToRight
                    ? -slideDistance
                    : slideDistance;

            double incomingOffset =
                movingToRight
                    ? slideDistance
                    : -slideDistance;

            UpdateTabAppearance(targetPanel);

            incomingPanel.TranslationX = incomingOffset;
            incomingPanel.Opacity = 0;
            incomingPanel.IsVisible = true;

            Task outgoingAnimation = Task.WhenAll(
                outgoingPanel.TranslateTo(
                    outgoingOffset,
                    0,
                    200,
                    Easing.CubicIn),

                outgoingPanel.FadeTo(
                    0,
                    160,
                    Easing.CubicIn));

            Task incomingAnimation = Task.WhenAll(
                incomingPanel.TranslateTo(
                    0,
                    0,
                    240,
                    Easing.CubicOut),

                incomingPanel.FadeTo(
                    1,
                    200,
                    Easing.CubicOut));

            await Task.WhenAll(
                outgoingAnimation,
                incomingAnimation);

            outgoingPanel.IsVisible = false;
            outgoingPanel.TranslationX = 0;
            outgoingPanel.Opacity = 1;

            incomingPanel.TranslationX = 0;
            incomingPanel.Opacity = 1;

            activePanel = targetPanel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Discover panel transition failed: {ex}");

            bool showCategories =
                targetPanel == DiscoverPanel.Categories;

            ExploreCategoriesPanel.CancelAnimations();
            RideHailingPanel.CancelAnimations();

            ExploreCategoriesPanel.IsVisible = showCategories;
            ExploreCategoriesPanel.TranslationX = 0;
            ExploreCategoriesPanel.Opacity = 1;

            RideHailingPanel.IsVisible = !showCategories;
            RideHailingPanel.TranslationX = 0;
            RideHailingPanel.Opacity = 1;

            activePanel = targetPanel;

            UpdateTabAppearance(targetPanel);
        }
        finally
        {
            isSwitchingPanel = false;
        }
    }

    private void UpdateTabAppearance(
        DiscoverPanel panel)
    {
        bool showCategories =
            panel == DiscoverPanel.Categories;

        ExploreTabSurface.BackgroundColor =
            showCategories
                ? PrimaryBlue
                : Colors.Transparent;

        RideTabSurface.BackgroundColor =
            showCategories
                ? Colors.Transparent
                : PrimaryBlue;

        ExploreTabButton.TextColor =
            showCategories
                ? Colors.White
                : DeepBlue;

        RideTabButton.TextColor =
            showCategories
                ? DeepBlue
                : Colors.White;
    }

    // ---------------------------------------------------------
    // RIDE HAILING PROVIDER CARDS
    // ---------------------------------------------------------


    private void RefreshRideHailingProviders()
    {
        RideHailingProvidersLayout.Children.Clear();

        // Reload the latest saved provider records.
        rideHailingProviderContentService.Reload();

        List<RideHailingProvider> providers =
            rideHailingProviderContentService.GetAllProviders();

        if (providers.Count == 0)
        {
            Label emptyState = new()
            {
                Text = "No ride-hailing providers are available yet.",
                FontSize = 14,
                TextColor = TextSecondary,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(12, 24),
                LineBreakMode = LineBreakMode.WordWrap
            };

            RideHailingProvidersLayout.Children.Add(emptyState);
            return;
        }

        // ---------------------------------------------------------
        // TWO-COLUMN PROVIDER GRID
        // ---------------------------------------------------------

        bool isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        Grid providersGrid = new()
        {
            ColumnSpacing = isPhone ? 12 : 24,
            RowSpacing = isPhone ? 12 : 24,
            Padding = isPhone ? 16 : 28,
            HorizontalOptions = LayoutOptions.Center
        };

        // Create two equal-width columns.
        providersGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Star
            });

        providersGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Star
            });

        // Add provider cards in pairs.
        for (int i = 0; i < providers.Count; i++)
        {
            View card =
                CreateRideHailingProviderCard(providers[i]);

            int row = i / 2;
            int column = i % 2;

            while (providersGrid.RowDefinitions.Count <= row)
            {
                providersGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = GridLength.Auto
                    });
            }

            Grid.SetRow(card, row);
            Grid.SetColumn(card, column);

            providersGrid.Children.Add(card);
        }

        RideHailingProvidersLayout.Children.Add(providersGrid);
    }

    private View CreateRideHailingProviderCard(
    RideHailingProvider provider)
    {
        bool isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        double cardWidth = isPhone ? 160 : 420;
        double cardHeight = isPhone ? 120 : 260;

        // Find the first existing provider photo.
        string? bannerPhoto = provider.Photos?
            .FirstOrDefault(photo =>
                !string.IsNullOrWhiteSpace(photo) &&
                File.Exists(photo));

        bool hasBannerPhoto =
            !string.IsNullOrWhiteSpace(bannerPhoto);

        // ---------------------------------------------------------
        // IMAGE
        // ---------------------------------------------------------

        Image bannerImage = new()
        {
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Color.FromArgb("#DCE6EE")
        };

        if (hasBannerPhoto)
        {
            bannerImage.Source =
                ImageSource.FromFile(bannerPhoto!);
        }

        Label fallbackSymbol = new()
        {
            Text = provider.PlaceholderSymbol,
            FontSize = isPhone ? 40 : 76,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = !hasBannerPhoto
        };

        // ---------------------------------------------------------
        // PROVIDER NAME STRIP
        // ---------------------------------------------------------

        Grid nameStrip = new()
        {
            BackgroundColor = Color.FromArgb("#CC203746"),
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill,
            HeightRequest = isPhone ? 44 : 62,
            Padding = new Thickness(
                isPhone ? 10 : 18,
                0,
                isPhone ? 8 : 14,
                0)
        };

        Label nameLabel = new()
        {
            Text = provider.Name,
            TextColor = Colors.White,
            FontSize = isPhone ? 16 : 25,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        nameStrip.Children.Add(nameLabel);

        // ---------------------------------------------------------
        // IMAGE CARD
        // ---------------------------------------------------------

        Grid imageContent = new()
        {
            BackgroundColor =
                Color.FromArgb(provider.PlaceholderColor)
        };

        imageContent.Children.Add(bannerImage);
        imageContent.Children.Add(fallbackSymbol);
        imageContent.Children.Add(nameStrip);

        Border imageCard = new()
        {
            Content = imageContent,
            WidthRequest = cardWidth,
            HeightRequest = cardHeight,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 17
            }
        };

        // ---------------------------------------------------------
        // PROVIDER INFORMATION
        // ---------------------------------------------------------

        VerticalStackLayout details = new()
        {
            Padding = new Thickness(
                isPhone ? 10 : 18,
                isPhone ? 8 : 14),
            Spacing = isPhone ? 4 : 8,
            WidthRequest = cardWidth
        };

        Label serviceType = new()
        {
            Text = provider.ServiceType,
            FontSize = isPhone ? 11 : 17,
            TextColor = TextSecondary,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        Label openDetails = new()
        {
            Text = "View provider details  ›",
            FontSize = isPhone ? 12 : 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = PrimaryBlue
        };

        details.Children.Add(serviceType);
        details.Children.Add(openDetails);

        // ---------------------------------------------------------
        // COMPLETE PROVIDER CARD
        // ---------------------------------------------------------

        VerticalStackLayout cardContent = new()
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.Center,
            Children =
        {
            imageCard,
            details
        }
        };

        Border card = new()
        {
            Content = cardContent,
            Padding = 0,
            Stroke = BorderBlue,
            StrokeThickness = 1,
            BackgroundColor = SurfaceWhite,
            HorizontalOptions = LayoutOptions.Center
        };

        card.StrokeShape = new RoundRectangle
        {
            CornerRadius = 16
        };

        // ---------------------------------------------------------
        // NAVIGATION
        // ---------------------------------------------------------

        TapGestureRecognizer tap = new();

        tap.Tapped += async (sender, args) =>
        {
            await Navigation.PushAsync(
                new RideHailingProviderDetailsPage(provider));
        };

        card.GestureRecognizers.Add(tap);

        SemanticProperties.SetDescription(
            card,
            $"View details for {provider.Name}");

        return card;
    }

    // ---------------------------------------------------------
    // FEATURED REGION
    // ---------------------------------------------------------

    private void OnFeaturedBannerSizeChanged(
        object? sender,
        EventArgs e)
    {
        // The banner currently has a fixed height in XAML.
    }

    public void SetSelectedRegion(string regionName)
    {
        if (string.IsNullOrWhiteSpace(regionName))
            return;

        selectedRegion = regionName.Trim();

        Preferences.Default.Set(
            SelectedRegionKey,
            selectedRegion);

        UpdateFeaturedRegion();
        UpdateBreadcrumbs();
        RefreshDiscoveryCategories();
    }

    private void UpdateFeaturedRegion()
    {
        FeaturedRegionLabel.Text =
            selectedRegion ?? "Select a region";

        regionMediaService.Reload();

        System.Diagnostics.Debug.WriteLine($"REGIONMEDIA selected='{selectedRegion}'");
        foreach (var m in regionMediaService.GetAll())
            System.Diagnostics.Debug.WriteLine($"REGIONMEDIA '{m.Region}' -> '{m.Photo}' exists={File.Exists(m.Photo)}");

        string? photoPath =
            regionMediaService.GetPhoto(selectedRegion);

        FeaturedRegionImage.Source =
            photoPath != null
                ? ImageSource.FromFile(photoPath)
                : null;
    }

    private async void OnFeaturedRegionClicked(
        object sender,
        EventArgs e)
    {
        List<Place> allPlaces =
            placeContentService.GetAllPlaces();

        await Navigation.PushAsync(
            new ChooseRegionPage(
                allPlaces,
                path =>
                {
                    if (!string.IsNullOrWhiteSpace(path.Region))
                    {
                        SetSelectedRegion(path.Region);
                    }
                }));
    }

    // ---------------------------------------------------------
    // BREADCRUMBS
    // ---------------------------------------------------------

    private void UpdateBreadcrumbs()
    {
        BreadcrumbBar.Children.Clear();

        AddBreadcrumb("Discover", false);

        if (!string.IsNullOrWhiteSpace(selectedRegion))
        {
            AddBreadcrumb("›", false);
            AddBreadcrumb(selectedRegion, true);
        }
    }

    private void AddBreadcrumb(
        string text,
        bool isCurrent)
    {
        Label label = new()
        {
            Text = text,
            FontSize = 12,
            FontAttributes = isCurrent
                ? FontAttributes.Bold
                : FontAttributes.None,

            TextColor = isCurrent
                ? DeepBlue
                : TextSecondary,

            VerticalOptions = LayoutOptions.Center
        };

        BreadcrumbBar.Children.Add(label);
    }

    // ---------------------------------------------------------
    // NEAR ME
    // ---------------------------------------------------------

    private async void OnNearMeClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            Location? location =
                await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest
                    {
                        DesiredAccuracy =
                            GeolocationAccuracy.Low
                    });

            if (location == null)
            {
                await DisplayAlertAsync(
                    "Location unavailable",
                    "We couldn't determine your current location.",
                    "OK");

                return;
            }

            string? region =
                FindNearestRegion(location);

            if (region == null)
            {
                await DisplayAlertAsync(
                    "Region unavailable",
                    "Your current location is outside the regions currently supported by LugarLang.",
                    "OK");

                return;
            }

            SetSelectedRegion(region);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Location error",
                ex.Message,
                "OK");
        }
    }

    private string? FindNearestRegion(
        Location location)
    {
        const double MaxKm = 100;

        var nearest = placeContentService
            .GetAllPlaces()
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Region) &&
                (p.Latitude != 0 || p.Longitude != 0))
            .Select(p => new
            {
                p.Region,

                Km = Location.CalculateDistance(
                    location.Latitude,
                    location.Longitude,
                    p.Latitude,
                    p.Longitude,
                    DistanceUnits.Kilometers)
            })
            .OrderBy(x => x.Km)
            .FirstOrDefault();

        return nearest != null && nearest.Km <= MaxKm
            ? nearest.Region
            : null;
    }

    // ---------------------------------------------------------
    // CATEGORY RETRIEVAL
    // ---------------------------------------------------------

    private List<DiscoveryCategory> GetEnabledCategories()
    {
        List<DiscoveryCategory> categories =
            categoryContentService.GetAllCategories();

        foreach (DiscoveryCategory category in categories)
        {
            System.Diagnostics.Debug.WriteLine(
                $"DISCOVER CATEGORY: " +
                $"Id={category.Id}, " +
                $"Name={category.Name}, " +
                $"Order={category.DisplayOrder}, " +
                $"Enabled={category.IsEnabled}");
        }

        return categories
            .Where(category => category.IsEnabled)
            .OrderBy(category => category.DisplayOrder)
            .ToList();
    }

    // ---------------------------------------------------------
    // PLACE CARDS
    // ---------------------------------------------------------

    private Grid CreatePlaceCard(
        Place place,
        HorizontalStackLayout parentLayout)
    {
        System.Diagnostics.Debug.WriteLine(
            $"PLACE ID: {place.Id} NAME: {place.Name}");

        Grid card = new()
        {
            WidthRequest = CardWidth,
            HeightRequest = CardHeight,
            BackgroundColor = CardPlaceholder
        };

        DeveloperEditable.SetEditableGroup(
            card,
            "category.carousel.tile");

        Image placeImage = new()
        {
            Aspect = Aspect.AspectFill,
            BackgroundColor = CardPlaceholder
        };

        string? firstPhoto = place.Photos?.FirstOrDefault();
        System.Diagnostics.Debug.WriteLine(
            $"TILEPHOTO '{place.Name}' count={place.Photos?.Count} " +
            $"first='{firstPhoto}' " +
            $"rooted={(firstPhoto != null && System.IO.Path.IsPathRooted(firstPhoto))} " +
            $"exists={(firstPhoto != null && File.Exists(firstPhoto))}");

        if (place.Photos?.Count > 0)
        {
            placeImage.Source =
                ImageSource.FromFile(place.Photos[0]);
        }

        Border imageBorder = new()
        {
            StrokeThickness = 0,
            BackgroundColor = CardPlaceholder,
            Content = placeImage,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 16
            }
        };

        Grid nameOverlay = new()
        {
            BackgroundColor = CardOverlay,
            VerticalOptions = LayoutOptions.End,
            HeightRequest = 46,
            Padding = new Thickness(12, 0)
        };

        Label nameLabel = new()
        {
            Text = place.Name,
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        nameOverlay.Children.Add(nameLabel);

        Button placeButton = new()
        {
            Text = string.Empty,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Padding = 0,
            CornerRadius = 16
        };

        Button editButton = new()
        {
            Text = "✎",
            FontSize = 17,
            WidthRequest = 38,
            HeightRequest = 38,
            Padding = 0,
            CornerRadius = 19,
            BackgroundColor = SurfaceWhite,
            TextColor = DeepBlue,
            BorderColor = BorderBlue,
            BorderWidth = 1,
            IsVisible = false,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 8, 8, 0)
        };

        placeButton.Clicked += async (sender, e) =>
        {
            if (!isDeveloperMode)
            {
                await Navigation.PushAsync(
                    new PlaceDetailsPage(place));

                return;
            }

            if (editButton.IsVisible)
            {
                editButton.IsVisible = false;
                selectedPlace = null;
                return;
            }

            selectedPlace = place;

            foreach (View view in parentLayout.Children)
            {
                if (view is not Grid otherCard)
                    continue;

                foreach (View child in otherCard.Children)
                {
                    if (child is Button otherButton &&
                        otherButton.Text == "✎")
                    {
                        otherButton.IsVisible = false;
                    }
                }
            }

            editButton.IsVisible = true;
        };

        editButton.Clicked += OnEditPlaceClicked;

        card.Children.Add(imageBorder);
        card.Children.Add(nameOverlay);
        card.Children.Add(placeButton);
        card.Children.Add(editButton);

        SemanticProperties.SetDescription(
            card,
            $"Open {place.Name}");

        return card;
    }

    private async void OnEditPlaceClicked(
        object sender,
        EventArgs e)
    {
        if (selectedPlace == null)
            return;

        await Navigation.PushAsync(
            new PlaceEditPage(
                selectedPlace,
                placeContentService));
    }

    // ---------------------------------------------------------
    // CATEGORY EDITING
    // ---------------------------------------------------------

    private async void OnEditCategoryClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.BindingContext
            is not DiscoveryCategory category)
            return;

        await Navigation.PushAsync(
            new EditCategoryPage(
                category,
                categoryContentService,
                placeContentService));
    }

    private async void OnDeleteCategoryClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.BindingContext
            is not DiscoveryCategory category)
            return;

        int tileCount = placeContentService
            .GetAllPlaces()
            .Count(p => p.CategoryId == category.Id);

        bool confirm = await DisplayAlertAsync(
            "Delete Category",
            $"Delete \"{category.Name}\" and its {tileCount} tile(s)? This can't be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        foreach (Place tile in placeContentService
            .GetAllPlaces()
            .Where(p => p.CategoryId == category.Id)
            .ToList())
        {
            placeContentService.RemovePlace(tile);
        }

        categoryContentService.RemoveCategory(category);

        RefreshDiscoveryCategories();
    }

    // ---------------------------------------------------------
    // CATEGORY SECTION
    // ---------------------------------------------------------

    private VerticalStackLayout CreateCategorySection(
        DiscoveryCategory category)
    {
        List<Place> places = placeContentService
            .GetAllPlaces()
            .Where(place =>
                place.CategoryId == category.Id &&
                string.Equals(
                    place.Region,
                    selectedRegion,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        VerticalStackLayout categorySection = new()
        {
            Spacing = 12
        };

        Grid categoryHeader = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 6
        };

        Label categoryTitle = new()
        {
            Text = category.Name,
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = TextPrimary,
            VerticalOptions = LayoutOptions.Center
        };

        BoxView headingAccent = new()
        {
            HeightRequest = 3,
            WidthRequest = 38,
            BackgroundColor = PrimaryBlue,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 3)
        };

        VerticalStackLayout titleGroup = new()
        {
            Spacing = 4,
            Children =
            {
                categoryTitle,
                headingAccent
            }
        };

        Button editCategoryButton = new()
        {
            Text = "✎",
            FontSize = 17,
            WidthRequest = 38,
            HeightRequest = 38,
            Padding = 0,
            CornerRadius = 19,
            BackgroundColor = SecondaryBlue,
            TextColor = DeepBlue,
            BindingContext = category,
            IsVisible = isDeveloperMode
        };

        Button deleteCategoryButton = new()
        {
            Text = "×",
            FontSize = 23,
            WidthRequest = 38,
            HeightRequest = 38,
            Padding = 0,
            CornerRadius = 19,
            BackgroundColor = SecondaryBlue,
            TextColor = DeepBlue,
            BindingContext = category,
            IsVisible = isDeveloperMode
        };

        editCategoryButton.Clicked += OnEditCategoryClicked;
        deleteCategoryButton.Clicked += OnDeleteCategoryClicked;

        categoryHeader.Add(titleGroup, 0, 0);
        categoryHeader.Add(editCategoryButton, 1, 0);
        categoryHeader.Add(deleteCategoryButton, 2, 0);

        HorizontalStackLayout categoryTiles = new()
        {
            Spacing = 14
        };

        ScrollView categoryScroll = new()
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility =
                ScrollBarVisibility.Never,
            VerticalScrollBarVisibility =
                ScrollBarVisibility.Never,
            Content = categoryTiles
        };

        foreach (Place place in places)
        {
            categoryTiles.Children.Add(
                CreatePlaceCard(place, categoryTiles));
        }

        if (places.Count == 0)
        {
            Border emptyCategory = new()
            {
                BackgroundColor = SecondaryBlue,
                StrokeThickness = 0,
                Padding = new Thickness(16, 18),
                Content = new Label
                {
                    Text = "No places in this category yet.",
                    FontSize = 13,
                    TextColor = TextSecondary
                }
            };

            emptyCategory.StrokeShape = new RoundRectangle
            {
                CornerRadius = 12
            };

            categorySection.Children.Add(categoryHeader);
            categorySection.Children.Add(emptyCategory);
        }
        else
        {
            categorySection.Children.Add(categoryHeader);
            categorySection.Children.Add(categoryScroll);
        }

        return categorySection;
    }

    // ---------------------------------------------------------
    // CONTENT REFRESH
    // ---------------------------------------------------------

    private void RefreshDiscoveryCategories()
    {
        categoryContentService.Reload();
        placeContentService.Reload();

        List<string> regions = GetRegionNames();

        if (regions.Count > 0 &&
            !regions.Contains(
                selectedRegion ?? string.Empty,
                StringComparer.OrdinalIgnoreCase))
        {
            selectedRegion = regions[0];

            Preferences.Default.Set(
                SelectedRegionKey,
                selectedRegion);
        }

        UpdateFeaturedRegion();
        UpdateBreadcrumbs();

        DiscoveryCategoriesLayout.Children.Clear();

        List<DiscoveryCategory> categories =
            GetEnabledCategories();

        foreach (DiscoveryCategory category in categories)
        {
            DiscoveryCategoriesLayout.Children.Add(
                CreateCategorySection(category));
        }

        if (categories.Count == 0 && !isDeveloperMode)
        {
            Border emptyState = new()
            {
                BackgroundColor = SurfaceWhite,
                Stroke = BorderBlue,
                StrokeThickness = 1,
                Padding = new Thickness(20),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Nothing to explore just yet",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = TextPrimary,
                            HorizontalTextAlignment =
                                TextAlignment.Center
                        },
                        new Label
                        {
                            Text = "Refresh your content or try again later.",
                            FontSize = 14,
                            TextColor = TextSecondary,
                            HorizontalTextAlignment =
                                TextAlignment.Center
                        }
                    }
                }
            };

            emptyState.StrokeShape = new RoundRectangle
            {
                CornerRadius = 16
            };

            DiscoveryCategoriesLayout.Children.Add(emptyState);
        }

        if (isDeveloperMode)
        {
            Button addCategoryButton = new()
            {
                Text = "+  Add Category",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 52,
                CornerRadius = 14,
                BackgroundColor = SecondaryBlue,
                TextColor = DeepBlue,
                BorderColor = BorderBlue,
                BorderWidth = 1
            };

            addCategoryButton.Clicked += OnAddCategoryClicked;

            DiscoveryCategoriesLayout.Children.Add(
                addCategoryButton);
        }
    }

    private async void OnAddCategoryClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AddCategoryPage(categoryContentService));
    }

    private List<string> GetRegionNames() =>
        placeContentService
            .GetAllPlaces()
            .Select(p => p.Region)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();

    public void Refresh()
    {
        RefreshDiscoveryCategories();
    }

    // ---------------------------------------------------------
    // PAGE LIFECYCLE
    // ---------------------------------------------------------


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        MapBottomSheetControl.ApplyPendingLocationSelection();

        RefreshDiscoveryCategories();
        RefreshRideHailingProviders();

        try
        {
#if DEV
        // Populate a fresh installation, but never overwrite
        // existing developer-managed local content.
        bool synced = await contentSyncService.SyncAsync(
            onlyIfLocalEmpty: true);
#else
            // Release builds check for updated server content.
            bool synced = await contentSyncService.SyncAsync();
#endif

            if (synced)
            {
                RefreshDiscoveryCategories();
                RefreshRideHailingProviders();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Initial content sync failed: {ex}");
        }
    }



    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
#if DEV
    // DEV: Reload locally managed content only.
    categoryContentService.Reload();
    placeContentService.Reload();
    regionMediaService.Reload();
    
    RefreshDiscoveryCategories();
    RefreshRideHailingProviders();

    System.Diagnostics.Debug.WriteLine(
        "DEV REFRESH: Reloaded local content.");

#else
        // RELEASE: Server is the source of truth.
        try
        {
            bool synced =
                await contentSyncService.SyncAsync(force: true);

            RefreshDiscoveryCategories();
            RefreshRideHailingProviders();

            if (!synced)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RELEASE REFRESH: No content changes or sync unavailable.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"RELEASE REFRESH FAILED: {ex}");
        }
#endif
    }


    private static string LoadSavedRegion()
    {
        string saved =
            Preferences.Default.Get(
                SelectedRegionKey,
                DefaultRegion);

        return string.IsNullOrWhiteSpace(saved)
            ? DefaultRegion
            : saved;
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        await SettingsMenu.OpenAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (SettingsMenu.IsOpen)
        {
            _ = SettingsMenu.CloseAsync();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
