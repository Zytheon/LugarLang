using LugarLang.Internal.Models;

namespace LugarLang.Mobile.Pages;

public partial class RideHailingProviderDetailsPage : ContentPage
{
    private readonly RideHailingProvider provider;

    public RideHailingProviderDetailsPage(
        RideHailingProvider provider)
    {
        InitializeComponent();

        this.provider = provider;

        Title = provider.Name;

        // Find the first existing provider photo.
        string? bannerPhoto = provider.Photos?
            .FirstOrDefault(photo =>
                !string.IsNullOrWhiteSpace(photo) &&
                File.Exists(photo));

        bool hasBannerPhoto =
            !string.IsNullOrWhiteSpace(bannerPhoto);

        // Set the fallback background.
        ProviderImageSurface.BackgroundColor =
            Color.FromArgb(provider.PlaceholderColor);

        // Display the uploaded photo when available.
        if (hasBannerPhoto)
        {
            ProviderBannerImage.Source =
                ImageSource.FromFile(bannerPhoto!);

            ProviderBannerImage.IsVisible = true;
        }
        else
        {
            ProviderBannerImage.Source = null;
            ProviderBannerImage.IsVisible = false;
        }

        // Keep the original placeholder as fallback.
        ProviderSymbolLabel.Text =
            provider.PlaceholderSymbol;

        ProviderSymbolLabel.IsVisible =
            !hasBannerPhoto;

        ProviderPlaceholderLabel.IsVisible =
            !hasBannerPhoto;

        // Provider information.
        ProviderNameLabel.Text =
            provider.Name;

        ProviderTypeLabel.Text =
            provider.ServiceType;

        ProviderDescriptionLabel.Text =
            provider.Description;

        ProviderCoverageLabel.Text =
            provider.Coverage;
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }
}