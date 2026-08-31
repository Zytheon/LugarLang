namespace LugarLang.Mobile.Pages;

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
//using Java.Lang.Reflect;
using LugarLang.Mobile.Controls.Developer;
using LugarLang.Mobile.Models;
using LugarLang.Mobile.Models.Discovery;
using LugarLang.Mobile.Pages.Developer;
using LugarLang.Mobile.Services;
using LugarLang.Mobile.Services.Content;
using LugarLang.Mobile.Services.Developer;

//using static KotlinX.Serialization.Descriptors.PrimitiveKind;

public partial class DiscoverPage : ContentPage
{
    private readonly RegionService regionService;
    private readonly CategoryContentService
    categoryContentService;

    private readonly RestaurantDiscoveryService
        restaurantDiscoveryService;
    private readonly PlaceContentService
    placeContentService;
    private readonly bool isDeveloperMode = true;

    private string? selectedRegion;
    private Place? selectedPlace;

    public DiscoverPage(
    bool isDeveloperMode = false)
{
    InitializeComponent();

    regionService =
        new RegionService();

    categoryContentService =
        new CategoryContentService();

        placeContentService =
        new PlaceContentService();

    restaurantDiscoveryService =
        new RestaurantDiscoveryService(
            placeContentService);

    selectedRegion =
        "Northern Mindanao";

#if DEV_TOOLS
        DeveloperOverlayManager developerOverlayManager =
            Microsoft.Maui.Controls.Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<DeveloperOverlayManager>();

        developerLauncherControl =
            new DeveloperLauncher
            {
                Margin = new Thickness(0, 0, 16, 16)
            };

        ((Grid)Content).Children.Add(
            developerLauncherControl);

        developerLauncherControl.EditUIClicked =
            async () => await LaunchDeveloperEditor(
                DeveloperEditorLaunchMode.Normal,
                developerOverlayManager);

        developerLauncherControl.MultiUIEditClicked =
            async () => await LaunchDeveloperEditor(
                DeveloperEditorLaunchMode.MultiSelect,
                developerOverlayManager);

        developerLauncherControl.ExportCategoriesRequested =
    async () => await ExportCategoriesAsync();

        developerLauncherControl.ExportPlacesRequested =
            async () => await ExportPlacesAsync();

        developerLauncherControl.AddUIElementClicked =
            async () => await LaunchDeveloperEditor(
                DeveloperEditorLaunchMode.AddElement,
                developerOverlayManager);

        developerLauncherControl.CommitRequested =
            developerOverlayManager.CommitAsync;
#endif

    }

#if DEV_TOOLS
    private DeveloperLauncher? developerLauncherControl;
#endif

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
                regionService.GetRegion(
                    location.Latitude,
                    location.Longitude);

            if (region == null)
            {
                await DisplayAlertAsync(
                    "Region unavailable",
                    "Your current location is outside the regions currently supported by LugarLang.",
                    "OK");

                return;
            }

            SetSelectedRegion(
                region);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Location error",
                ex.Message,
                "OK");
        }
    }

    public void SetSelectedRegion(
        string regionName)
    {
        selectedRegion =
            regionName;

        FeaturedRegionButton.Text =
            regionName;

        RefreshDiscoveryCategories();
    }

    private async void OnFeaturedRegionClicked(
    object sender,
    EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            "NORTHERN MINDANAO CLICKED");

        await Navigation.PushAsync(
            new ChooseRegionPage(this));
    }


#if DEV_TOOLS
    private async Task LaunchDeveloperEditor(
        DeveloperEditorLaunchMode launchMode,
        DeveloperOverlayManager developerOverlayManager)
    {
        const double manualOffsetAdjustment = -32;

        double verticalOffset =
#if WINDOWS
    DeveloperScreenPosition.GetTopOffset(this.Content!) + manualOffsetAdjustment;
#else
            manualOffsetAdjustment;
#endif

        DeveloperEditorModalPage modalPage =
            new DeveloperEditorModalPage(
                this.Content!,
                verticalOffset,
                launchMode,
                (element, x, y) =>
                    developerOverlayManager.RecordLayoutChange(
                        this,
                        this.Content!,
                        element,
                        x,
                        y),
                (element, width, height) =>
                    developerOverlayManager.RecordLayoutChange(
                        this,
                        this.Content!,
                        element,
                        element.TranslationX,
                        element.TranslationY,
                        width,
                        height));

        await Navigation.PushModalAsync(
            modalPage,
            animated: false);
    }
#endif

#if DEV_TOOLS
    private async Task ExportCategoriesAsync()
    {
        string json =
            categoryContentService.ExportToJson();

        using MemoryStream stream =
            new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(json));

        FileSaverResult result =
            await FileSaver.Default.SaveAsync(
                "seed_categories.json",
                stream,
                CancellationToken.None);

        if (result.IsSuccessful)
        {
            await DisplayAlertAsync(
                "Export Complete",
                $"Saved to:\n{result.FilePath}",
                "OK");
        }
        else
        {
            await DisplayAlertAsync(
                "Export Failed",
                result.Exception?.Message ?? "Unknown error",
                "OK");
        }
    }

    private async Task ExportPlacesAsync()
    {
        string json =
            placeContentService.ExportToJson();

        using MemoryStream stream =
            new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(json));

        FileSaverResult result =
            await FileSaver.Default.SaveAsync(
                "seed_places.json",
                stream,
                CancellationToken.None);

        if (result.IsSuccessful)
        {
            await DisplayAlertAsync(
                "Export Complete",
                $"Saved to:\n{result.FilePath}",
                "OK");
        }
        else
        {
            await DisplayAlertAsync(
                "Export Failed",
                result.Exception?.Message ?? "Unknown error",
                "OK");
        }
    }
#endif

    private List<DiscoveryCategory> GetEnabledCategories()
    {
        List<DiscoveryCategory> categories =
            categoryContentService
                .GetAllCategories();

        foreach (
            DiscoveryCategory category
            in categories)
        {
            System.Diagnostics.Debug.WriteLine(
                $"DISCOVER CATEGORY: " +
                $"Id={category.Id}, " +
                $"Name={category.Name}, " +
                $"Order={category.DisplayOrder}, " +
                $"Enabled={category.IsEnabled}");
        }

        return categories
            .Where(
                category =>
                    category.IsEnabled)
            .ToList();
    }

    
    private Grid CreatePlaceCard(
    Place place,
    HorizontalStackLayout
        parentLayout)
    {
        Grid card =
            new Grid
            {
                WidthRequest = 180,
                HeightRequest = 140
            };

        Image placeImage =
            new Image
            {
                Aspect =
                    Aspect.AspectFill
            };

        DeveloperEditable.SetEditableGroup(
    card,
    "category.carousel.tile");

        if (place.Photos.Count > 0)
        {
            placeImage.Source =
                ImageSource.FromFile(
                    place.Photos[0]);
        }

        Border imageBorder =
            new Border
            {
                StrokeThickness = 0,
                Content = placeImage
            };

        Grid nameOverlay =
            new Grid
            {
                BackgroundColor =
                    Colors.Black.WithAlpha(
                        0.55f),

                VerticalOptions =
                    LayoutOptions.End,

                HeightRequest = 42
            };

        Label nameLabel =
            new Label
            {
                Text = place.Name,
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes =
                    FontAttributes.Bold,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                HorizontalTextAlignment =
                    TextAlignment.Center
            };

        nameOverlay.Children.Add(
            nameLabel);

        Button placeButton =
            new Button
            {
                BackgroundColor =
                    Colors.Transparent,

                BorderWidth = 0
            };

        Button editButton =
            new Button
            {
                Text = "✎",
                FontSize = 18,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                IsVisible = false,

                HorizontalOptions =
                    LayoutOptions.End,

                VerticalOptions =
                    LayoutOptions.End
            };

        placeButton.Clicked +=
            async (sender, e) =>
            {
                if (!isDeveloperMode)
                {
                    await Navigation.PushAsync(
                        new PlaceDetailsPage(
                            place));

                    return;
                }

                if (editButton.IsVisible)
                {
                    editButton.IsVisible = false;
                    selectedPlace = null;

                    return;
                }

                selectedPlace = place;

                foreach (
                    View view
                    in parentLayout.Children)
                {
                    if (view is not Grid otherCard)
                    {
                        continue;
                    }

                    foreach (
                        View child
                        in otherCard.Children)
                    {
                        if (
                            child is Button otherButton &&
                            otherButton.Text == "✎")
                        {
                            otherButton.IsVisible =
                                false;
                        }
                    }
                }

                editButton.IsVisible = true;
            };

        editButton.Clicked +=
            OnEditPlaceClicked;

        card.Children.Add(
            imageBorder);

        card.Children.Add(
            nameOverlay);

        card.Children.Add(
            placeButton);

        card.Children.Add(
            editButton);

        return card;
    }

    private async void OnEditPlaceClicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        await Navigation.PushAsync(
            new PlaceEditPage(
                selectedPlace,
                placeContentService));
    }

    private async void OnEditCategoryClicked(
        object? sender,
        EventArgs e)
    {

        if (sender is not Button button)
        {
            return;
        }

        if (button.BindingContext
            is not DiscoveryCategory category)
        {
            return;
        }

        await Navigation.PushAsync(
            new EditCategoryPage(
                category,
                categoryContentService));
    }

    private async void OnDeleteCategoryClicked(
        object? sender,
        EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.BindingContext
            is not DiscoveryCategory category)
        {
            return;
        }

        bool confirm =
            await DisplayAlertAsync(
                "Delete Category",
                $"Are you sure you want to permanently delete \"{category.Name}\"?",
                "Delete",
                "Cancel");

        if (!confirm)
        {
            return;
        }

        categoryContentService.RemoveCategory(
            category);

        RefreshDiscoveryCategories();
    }

    private VerticalStackLayout CreateCategorySection(
    DiscoveryCategory category)
    {
        List<Place> places =
            placeContentService
                .GetAllPlaces()
                .Where(
                    place =>
                        place.CategoryId == category.Id &&
                        string.Equals(
                            place.Region,
                            selectedRegion,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        VerticalStackLayout categorySection =
            new VerticalStackLayout
            {
                Spacing = 12
            };

        Grid categoryHeader =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitionCollection
                    {
                    new ColumnDefinition(
                        GridLength.Star),

                    new ColumnDefinition(
                        GridLength.Auto),

                    new ColumnDefinition(
                        GridLength.Auto)
                    },

                ColumnSpacing = 6
            };

        Label categoryTitle =
            new Label
            {
                Text = category.Name,
                FontSize = 22,
                FontAttributes =
                    FontAttributes.Bold,
                VerticalOptions =
                    LayoutOptions.Center
            };

        Button editCategoryButton =
            new Button
            {
                Text = "✎",
                FontSize = 18,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                BindingContext = category,
                IsVisible = isDeveloperMode
            };

        Button deleteCategoryButton =
            new Button
            {
                Text = "🗑",
                FontSize = 18,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                BindingContext = category,
                IsVisible = isDeveloperMode
            };

        editCategoryButton.Clicked +=
            OnEditCategoryClicked;

        deleteCategoryButton.Clicked +=
            OnDeleteCategoryClicked;

        categoryHeader.Children.Add(
            categoryTitle);

        Grid.SetColumn(
            editCategoryButton,
            1);

        categoryHeader.Children.Add(
            editCategoryButton);

        Grid.SetColumn(
            deleteCategoryButton,
            2);

        categoryHeader.Children.Add(
            deleteCategoryButton);

        HorizontalStackLayout categoryTiles =
            new HorizontalStackLayout
            {
                Spacing = 12
            };

        ScrollView categoryScroll =
            new ScrollView
            {
                Orientation =
                    ScrollOrientation.Horizontal,

                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Never,

                Content =
                    categoryTiles
            };

        foreach (
            Place place
            in places)
        {
            Grid card =
                CreatePlaceCard(
                    place,
                    categoryTiles);

            categoryTiles.Children.Add(
                card);
        }

        categorySection.Children.Add(
            categoryHeader);

        categorySection.Children.Add(
            categoryScroll);

        return categorySection;
    }

    private void RefreshDiscoveryCategories()
    {
        categoryContentService.Reload();
        placeContentService.Reload();

        DiscoveryCategoriesLayout.Children.Clear();

        List<DiscoveryCategory> categories =
            GetEnabledCategories();

        foreach (
            DiscoveryCategory category
            in categories)
        {
            DiscoveryCategoriesLayout.Children.Add(
                CreateCategorySection(
                    category));
        }

        if (isDeveloperMode)
        {
            Button addCategoryButton =
                new Button
                {
                    Text = "+ Add Category",
                    FontSize = 18,
                    HeightRequest = 55
                };

            addCategoryButton.Clicked +=
                OnAddCategoryClicked;

            DiscoveryCategoriesLayout.Children.Add(
                addCategoryButton);
        }
    }


    public void Refresh()
    {
        RefreshDiscoveryCategories();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine(
            "DISCOVER ONAPPEARING FIRED");

        RefreshDiscoveryCategories();

#if DEV_TOOLS
        DeveloperOverlayManager developerOverlayManager =
            Microsoft.Maui.Controls.Application.Current!
                .Handler!
                .MauiContext!
                .Services
                .GetRequiredService<DeveloperOverlayManager>();

        developerOverlayManager.RestoreLayout(
            this,
            this.Content!);
#endif
    }

    private async void OnAddCategoryClicked(
        object? sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AddCategoryPage(
                categoryContentService));
    }

    private void OnRefreshClicked(
        object? sender,
        EventArgs e)
    {
        categoryContentService.Reload();

        placeContentService.Reload();

        RefreshDiscoveryCategories();
    }


}