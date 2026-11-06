
using LugarLang.Internal.Models;
using LugarLang.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;
namespace LugarLang.Mobile.Pages;

public partial class RideHailingManagementPage : ContentPage
{
    private readonly RideHailingProviderContentService providerService;

    public RideHailingManagementPage(
        RideHailingProviderContentService providerService)
    {
        InitializeComponent();

        this.providerService = providerService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        RefreshProviders();
    }

    private void RefreshProviders()
    {
        ProvidersLayout.Children.Clear();

        List<RideHailingProvider> providers =
            providerService.GetAllProviders();

        EmptyStateLabel.IsVisible = providers.Count == 0;

        foreach (RideHailingProvider provider in providers)
        {
            ProvidersLayout.Children.Add(
                CreateProviderCard(provider));
        }
    }

    private View CreateProviderCard(
        RideHailingProvider provider)
    {
        Color backgroundColor = GetProviderColor(
            provider.PlaceholderColor);

        var symbolLabel = new Label
        {
            Text = provider.PlaceholderSymbol,
            FontSize = 32,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var nameLabel = new Label
        {
            Text = provider.Name,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#253B50")
        };

        var typeLabel = new Label
        {
            Text = provider.ServiceType,
            FontSize = 13,
            TextColor = Color.FromArgb("#627B92")
        };

        var coverageLabel = new Label
        {
            Text = provider.Coverage,
            FontSize = 12,
            TextColor = Color.FromArgb("#627B92"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        var details = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                nameLabel,
                typeLabel,
                coverageLabel
            }
        };

        var editButton = new Button
        {
            Text = "Edit",
            BackgroundColor = Color.FromArgb("#E5EEF6"),
            TextColor = Color.FromArgb("#34566F"),
            CornerRadius = 10,
            Padding = new Thickness(14, 8)
        };

        editButton.Clicked += async (sender, args) =>
        {
            await OpenEditor(provider);
        };

        var deleteButton = new Button
        {
            Text = "Delete",
            BackgroundColor = Color.FromArgb("#FCE8E8"),
            TextColor = Color.FromArgb("#A33B3B"),
            CornerRadius = 10,
            Padding = new Thickness(14, 8)
        };

        deleteButton.Clicked += async (sender, args) =>
        {
            await DeleteProvider(provider);
        };

        var actionLayout = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                editButton,
                deleteButton
            }
        };


        var providerHeader = new Grid
        {
            ColumnDefinitions =
    {
        new ColumnDefinition(GridLength.Auto),
        new ColumnDefinition(GridLength.Star)
    },
            ColumnSpacing = 14
        };

        var symbolSurface = new Border
        {
            WidthRequest = 64,
            HeightRequest = 64,
            BackgroundColor = backgroundColor,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 14
            },
            Content = symbolLabel
        };

        providerHeader.Children.Add(symbolSurface);

        Grid.SetColumn(details, 1);
        providerHeader.Children.Add(details);

        var cardLayout = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
    {
        providerHeader,
        actionLayout
    }
        };


        return new Border
        {
            Padding = 14,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#D6E2ED"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 16
            },
            Content = cardLayout
        };
    }

    private async Task OpenEditor(
        RideHailingProvider? provider)
    {
        await Navigation.PushAsync(
            new RideHailingProviderEditPage(
                providerService,
                provider));
    }

    private async Task DeleteProvider(
        RideHailingProvider provider)
    {
        bool confirmed = await DisplayAlert(
            "Delete Provider",
            $"Are you sure you want to delete {provider.Name}?",
            "Delete",
            "Cancel");

        if (!confirmed)
            return;

        providerService.RemoveProvider(provider.Id);

        RefreshProviders();
    }

    private async void OnAddProviderClicked(
        object sender,
        EventArgs e)
    {
        await OpenEditor(null);
    }

    private static Color GetProviderColor(string color)
    {
        try
        {
            return Color.FromArgb(color);
        }
        catch
        {
            return Color.FromArgb("#486F91");
        }
    }
}
