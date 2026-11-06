
using LugarLang.Internal.Models;
using LugarLang.Internal.Services;
using LugarLang.Internal.Services.Content;
using LugarLang.Mobile.Services;

using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace LugarLang.Mobile.Pages;

public partial class RideHailingProviderEditPage : ContentPage
{
    private readonly RideHailingProviderContentService providerService;
    private readonly RideHailingProvider? existingProvider;
    private readonly LocationOptionsService locationOptions;

    private const string AddNewItem = "+ Add new…";

    private bool loadingPickers;
    private bool changesSaved;

    // Map
    private MapControl? pickerMapControl;
    private MemoryLayer? pickerPinLayer;
    private double? pendingLatitude;
    private double? pendingLongitude;

    // Photos
    private readonly List<string> photos = new();
    private readonly List<string> newlyCopiedPhotoPaths = new();

    public RideHailingProviderEditPage(
        RideHailingProviderContentService providerService,
        RideHailingProvider? provider = null)
    {
        InitializeComponent();

        this.providerService = providerService;
        existingProvider = provider;

        var services =
            App.Current!.Handler!.MauiContext!.Services;

        var placeContentService =
            services.GetRequiredService<PlaceContentService>();

        locationOptions =
            new LocationOptionsService(
                placeContentService.GetAllPlaces());

        if (provider != null)
        {
            InitializeProviderFields(provider);
            InitializeExistingLocation(provider);
            InitializeExistingPhotos(provider);
        }
        else
        {
            InitializeNewProvider();
        }

        InitializeLocationPickers(provider);
        RefreshPhotoDisplay();
        UpdateLocationStatus();
    }

    // ---------------------------------------------------------
    // PROVIDER INFORMATION
    // ---------------------------------------------------------

    private void InitializeProviderFields(
        RideHailingProvider provider)
    {
        PageHeading.Text = "Edit Provider";
        Title = "Edit Provider";

        NameEntry.Text = provider.Name;
        ServiceTypeEntry.Text = provider.ServiceType;
        DescriptionEditor.Text = provider.Description;
        CoverageEditor.Text = provider.Coverage;
        SymbolEntry.Text = provider.PlaceholderSymbol;
        ColorEntry.Text = provider.PlaceholderColor;
    }

    private void InitializeNewProvider()
    {
        PageHeading.Text = "Add Provider";
        Title = "Add Provider";

        CoverageEditor.Text =
            "Coverage not yet verified.";

        SymbolEntry.Text = "🚗";
        ColorEntry.Text = "#486F91";
    }

    // ---------------------------------------------------------
    // LOCATION PICKERS
    // ---------------------------------------------------------

    private void InitializeExistingLocation(
        RideHailingProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(provider.Region))
        {
            locationOptions.AddRegion(provider.Region);
        }

        if (!string.IsNullOrWhiteSpace(provider.Region) &&
            !string.IsNullOrWhiteSpace(provider.Province))
        {
            locationOptions.AddProvince(
                provider.Region,
                provider.Province);
        }

        if (!string.IsNullOrWhiteSpace(provider.Region) &&
            !string.IsNullOrWhiteSpace(provider.Province) &&
            !string.IsNullOrWhiteSpace(provider.City))
        {
            locationOptions.AddCity(
                provider.Region,
                provider.Province,
                provider.City);
        }

        if (provider.Latitude.HasValue &&
            provider.Longitude.HasValue)
        {
            pendingLatitude = provider.Latitude.Value;
            pendingLongitude = provider.Longitude.Value;
        }
    }

    private void InitializeLocationPickers(
        RideHailingProvider? provider)
    {
        FillPicker(
            RegionPicker,
            locationOptions.Regions,
            provider?.Region);

        FillPicker(
            ProvincePicker,
            locationOptions.Provinces(provider?.Region),
            provider?.Province);

        FillPicker(
            CityPicker,
            locationOptions.Cities(
                provider?.Region,
                provider?.Province),
            provider?.City);
    }

    private static string? Selected(Picker picker)
    {
        return picker.SelectedItem is string value &&
               value != AddNewItem
            ? value
            : null;
    }

    private void FillPicker(
        Picker picker,
        IEnumerable<string> items,
        string? selected)
    {
        List<string> list = items.ToList();
        list.Add(AddNewItem);

        loadingPickers = true;

        picker.ItemsSource = list;

        picker.SelectedIndex = list.FindIndex(
            item => string.Equals(
                item,
                selected?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        loadingPickers = false;
    }

    private async void OnRegionChanged(
        object? sender,
        EventArgs e)
    {
        if (loadingPickers)
            return;

        if (RegionPicker.SelectedItem as string == AddNewItem)
        {
            string? name =
                await DisplayPromptAsync(
                    "Add region",
                    "Region name");

            if (!string.IsNullOrWhiteSpace(name))
            {
                locationOptions.AddRegion(name.Trim());
            }

            FillPicker(
                RegionPicker,
                locationOptions.Regions,
                name?.Trim());
        }

        string? region = Selected(RegionPicker);

        FillPicker(
            ProvincePicker,
            locationOptions.Provinces(region),
            null);

        FillPicker(
            CityPicker,
            locationOptions.Cities(region, null),
            null);
    }

    private async void OnProvinceChanged(
        object? sender,
        EventArgs e)
    {
        if (loadingPickers)
            return;

        string? region = Selected(RegionPicker);

        if (ProvincePicker.SelectedItem as string == AddNewItem)
        {
            if (region == null)
            {
                await DisplayAlert(
                    "Pick a region first",
                    "Choose a region before adding a province.",
                    "OK");

                FillPicker(
                    ProvincePicker,
                    locationOptions.Provinces(null),
                    null);

                return;
            }

            string? name =
                await DisplayPromptAsync(
                    "Add province",
                    "Province name");

            if (!string.IsNullOrWhiteSpace(name))
            {
                locationOptions.AddProvince(
                    region,
                    name.Trim());
            }

            FillPicker(
                ProvincePicker,
                locationOptions.Provinces(region),
                name?.Trim());
        }

        FillPicker(
            CityPicker,
            locationOptions.Cities(
                region,
                Selected(ProvincePicker)),
            null);
    }

    private async void OnCityChanged(
        object? sender,
        EventArgs e)
    {
        if (loadingPickers)
            return;

        if (CityPicker.SelectedItem as string != AddNewItem)
            return;

        string? region = Selected(RegionPicker);
        string? province = Selected(ProvincePicker);

        if (region == null || province == null)
        {
            await DisplayAlert(
                "Pick a province first",
                "Choose a region and province before adding a city.",
                "OK");

            FillPicker(
                CityPicker,
                locationOptions.Cities(region, province),
                null);

            return;
        }

        string? name =
            await DisplayPromptAsync(
                "Add city",
                "City name");

        if (!string.IsNullOrWhiteSpace(name))
        {
            locationOptions.AddCity(
                region,
                province,
                name.Trim());
        }

        FillPicker(
            CityPicker,
            locationOptions.Cities(region, province),
            name?.Trim());
    }

    // ---------------------------------------------------------
    // MAP LOCATION
    // ---------------------------------------------------------

    private (double lat, double lon)? CurrentLocation()
    {
        if (pendingLatitude is double latitude &&
            pendingLongitude is double longitude)
        {
            return (latitude, longitude);
        }

        if (existingProvider?.Latitude is double savedLatitude &&
            existingProvider.Longitude is double savedLongitude)
        {
            return (savedLatitude, savedLongitude);
        }

        return null;
    }

    private void UpdateLocationStatus()
    {
        var location = CurrentLocation();

        if (location == null)
        {
            LocationStatusLabel.Text =
                "No map location selected.";

            return;
        }

        LocationStatusLabel.Text =
            $"Selected location: " +
            $"{location.Value.lat.ToString("0.######", CultureInfo.InvariantCulture)}, " +
            $"{location.Value.lon.ToString("0.######", CultureInfo.InvariantCulture)}";
    }

    private void EnsurePickerMap()
    {
        if (pickerMapControl != null)
            return;

        pickerMapControl = new MapControl();

        pickerMapControl.Map?.Layers.Add(
            OpenStreetMap.CreateTileLayer(
                "LugarLang/1.0 (fernandicokenrich@gmail.com)"));

        var location = CurrentLocation();

        double longitude = location?.lon ?? 124.6319;
        double latitude = location?.lat ?? 8.4542;

        var center =
            Mapsui.Projections.SphericalMercator.FromLonLat(
                longitude,
                latitude);

        pickerMapControl.Map?.Navigator.CenterOnAndZoomTo(
            new MPoint(center.x, center.y),
            location != null ? 5 : 100);

        pickerMapControl.Info += OnPickerMapInfo;

        if (location != null)
        {
            ShowPin(center.x, center.y);
        }

        MapPinPlaceholder.Content = pickerMapControl;
    }

    private void OnPickOnMapClicked(
        object sender,
        EventArgs e)
    {
        EnsurePickerMap();

        MapPinPlaceholder.IsVisible =
            !MapPinPlaceholder.IsVisible;

        MapToggleButton.Text =
            MapPinPlaceholder.IsVisible
                ? "Hide Map"
                : "Pick Location on Map";
    }

    private void OnPickerMapInfo(
        object? sender,
        MapInfoEventArgs e)
    {
        var geographic =
            Mapsui.Projections.SphericalMercator.ToLonLat(
                e.WorldPosition.X,
                e.WorldPosition.Y);

        pendingLatitude = geographic.lat;
        pendingLongitude = geographic.lon;

        ShowPin(
            e.WorldPosition.X,
            e.WorldPosition.Y);

        UpdateLocationStatus();
    }

    private void ShowPin(
        double worldX,
        double worldY)
    {
        if (pickerPinLayer != null)
        {
            pickerMapControl?.Map?.Layers.Remove(
                pickerPinLayer);
        }

        var mapPoint =
            new NetTopologySuite.Geometries.Point(
                worldX,
                worldY);

        var feature = new GeometryFeature
        {
            Geometry = mapPoint
        };

        feature.Styles.Add(
            new SymbolStyle
            {
                SymbolScale = 0.8,

                Fill = new Mapsui.Styles.Brush(
                    Mapsui.Styles.Color.Red),

                Outline = new Pen(
                    Mapsui.Styles.Color.White,
                    2)
            });

        pickerPinLayer = new MemoryLayer
        {
            Name = "Provider Location",

            Features = new List<IFeature>
            {
                feature
            }
        };

        pickerMapControl?.Map?.Layers.Add(
            pickerPinLayer);
    }

    // ---------------------------------------------------------
    // PROVIDER PHOTOS
    // ---------------------------------------------------------

    private void InitializeExistingPhotos(
        RideHailingProvider provider)
    {
        photos.Clear();

        if (provider.Photos != null)
        {
            photos.AddRange(provider.Photos);
        }
    }

    private async void OnAddPhotoClicked(
        object sender,
        EventArgs e)
    {
        FileResult? result =
            await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select a provider photo"
                });

        if (result == null)
            return;

        string extension =
            Path.GetExtension(result.FileName).ToLowerInvariant();

        if (extension is not (
            ".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            await DisplayAlert(
                "Unsupported file",
                "Choose a .jpg, .jpeg, .png, or .webp image.",
                "OK");

            return;
        }

        string destination =
            Path.Combine(
                FileSystem.AppDataDirectory,
                $"{Guid.NewGuid()}{extension}");

        try
        {
            using Stream source =
                await result.OpenReadAsync();

            using FileStream target =
                File.Create(destination);

            await source.CopyToAsync(target);

            photos.Add(destination);
            newlyCopiedPhotoPaths.Add(destination);

            RefreshPhotoDisplay();
        }
        catch (Exception ex)
        {
            if (File.Exists(destination))
                File.Delete(destination);

            await DisplayAlert(
                "Photo Error",
                ex.Message,
                "OK");
        }
    }

    private void RefreshPhotoDisplay()
    {
        PhotosContainer.Children.Clear();

        PhotoCountLabel.Text =
            $"{photos.Count} photo{(photos.Count == 1 ? "" : "s")}";

        for (int i = 0; i < photos.Count; i++)
        {
            string photoPath = photos[i];

            var image = new Microsoft.Maui.Controls.Image
            {
                Source = ImageSource.FromFile(photoPath),
                HeightRequest = 150,
                Aspect = Aspect.AspectFill
            };

            var removeButton = new Button
            {
                Text = "Remove Photo",
                HeightRequest = 40,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E5EEF6"),
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#34566F"),
                CornerRadius = 10
            };

            removeButton.Clicked += (sender, args) =>
            {
                photos.Remove(photoPath);
                RefreshPhotoDisplay();
            };

            var photoLayout = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    image,
                    removeButton
                }
            };

            PhotosContainer.Children.Add(photoLayout);
        }
    }

    // ---------------------------------------------------------
    // SAVE
    // ---------------------------------------------------------

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        string name =
            NameEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert(
                "Missing Information",
                "Please enter the provider name.",
                "OK");

            return;
        }

        string? region = Selected(RegionPicker);

        if (region == null)
        {
            await DisplayAlert(
                "Missing Region",
                "Please select a region for this provider.",
                "OK");

            return;
        }

        string color =
            ColorEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(color))
            color = "#486F91";

        try
        {
            Microsoft.Maui.Graphics.Color.FromArgb(color);
        }
        catch
        {
            await DisplayAlert(
                "Invalid Color",
                "Please enter a valid hexadecimal color, such as #486F91.",
                "OK");

            return;
        }

        var location = CurrentLocation();

        var updatedProvider = new RideHailingProvider
        {
            Id = existingProvider?.Id ??
                 Guid.NewGuid().ToString(),

            Name = name,

            Description =
                DescriptionEditor.Text?.Trim() ?? string.Empty,

            ServiceType =
                ServiceTypeEntry.Text?.Trim() ?? string.Empty,

            Coverage =
                string.IsNullOrWhiteSpace(CoverageEditor.Text)
                    ? "Coverage not yet verified."
                    : CoverageEditor.Text.Trim(),

            PlaceholderSymbol =
                string.IsNullOrWhiteSpace(SymbolEntry.Text)
                    ? "🚗"
                    : SymbolEntry.Text.Trim(),

            PlaceholderColor = color,

            Region = region,

            Province = Selected(ProvincePicker) ?? string.Empty,

            City = Selected(CityPicker) ?? string.Empty,

            Latitude = location?.lat,

            Longitude = location?.lon,

            Photos = photos.ToList()
        };

        try
        {
            if (existingProvider == null)
            {
                providerService.AddProvider(updatedProvider);
            }
            else
            {
                providerService.UpdateProvider(updatedProvider);
            }

            changesSaved = true;

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Save Failed",
                ex.Message,
                "OK");
        }
    }

    // ---------------------------------------------------------
    // CANCEL
    // ---------------------------------------------------------

    private async void OnCancelClicked(
        object sender,
        EventArgs e)
    {
        foreach (string path in newlyCopiedPhotoPaths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        await Navigation.PopAsync();
    }
}
