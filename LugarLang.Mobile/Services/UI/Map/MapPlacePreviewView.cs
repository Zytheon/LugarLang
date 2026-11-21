
using Microsoft.Maui.Controls.Shapes;
using PlaceModel = LugarLang.Internal.Models.Place;

namespace LugarLang.Mobile.UI.Map;

public sealed class MapPlacePreviewView : ContentView
{
    private readonly Image photo;
    private readonly Label captionLabel;
    private readonly Label nameLabel;
    private readonly Label categoryLabel;
    private readonly Label descriptionLabel;
    private readonly Button detailsButton;

    private PlaceModel? currentPlace;

    public event EventHandler? CloseRequested;
    public event EventHandler<PlaceModel>? DetailsRequested;

    public MapPlacePreviewView()
    {
        Button closeButton = new()
        {
            Text = "×",
            FontSize = 23,
            WidthRequest = 40,
            HeightRequest = 40,
            Padding = 0,
            CornerRadius = 20,
            BackgroundColor = Color.FromArgb("#F1F6FA"),
            TextColor = Color.FromArgb("#34566F"),
            HorizontalOptions = LayoutOptions.End
        };

        closeButton.Clicked +=
            (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        photo = new Image
        {
            Aspect = Aspect.AspectFill,
            HeightRequest = 155,
            BackgroundColor = Color.FromArgb("#E5EEF6")
        };

        captionLabel = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#627B92")
        };

        nameLabel = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#253B50"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };

        categoryLabel = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#627B92")
        };

        descriptionLabel = new Label
        {
            FontSize = 13,
            TextColor = Color.FromArgb("#253B50"),
            MaxLines = 3,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        detailsButton = new Button
        {
            Text = "View full details",
            FontSize = 13,
            HeightRequest = 44,
            CornerRadius = 10,
            BackgroundColor = Color.FromArgb("#486F91"),
            TextColor = Colors.White
        };

        detailsButton.Clicked +=
            (_, _) =>
            {
                if (currentPlace != null)
                    DetailsRequested?.Invoke(this, currentPlace);
            };

        VerticalStackLayout content = new()
        {
            Spacing = 8,
            Padding = 12,
            Children =
            {
                closeButton,
                photo,
                captionLabel,
                nameLabel,
                categoryLabel,
                descriptionLabel,
                detailsButton
            }
        };

        Content = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#D6E2ED"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 16
            },
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Opacity = 0.20f,
                Radius = 12,
                Offset = new Point(0, 4)
            },
            Content = content
        };

        WidthRequest = 290;
        MaximumWidthRequest = 340;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(12);
        IsVisible = false;
    }

    public void Show(
        PlaceModel place,
        string caption)
    {
        currentPlace = place;

        captionLabel.Text = caption;
        nameLabel.Text = place.Name;

        categoryLabel.Text =
            string.IsNullOrWhiteSpace(place.Category)
                ? "Place"
                : place.Category;

        descriptionLabel.Text =
            string.IsNullOrWhiteSpace(place.Description)
                ? "No description available."
                : place.Description;

        string? photoPath =
            place.Photos?
                .FirstOrDefault(path =>
                    !string.IsNullOrWhiteSpace(path) &&
                    File.Exists(path));

        if (!string.IsNullOrWhiteSpace(photoPath))
        {
            photo.Source = ImageSource.FromFile(photoPath);
            photo.IsVisible = true;
        }
        else
        {
            photo.Source = null;
            photo.IsVisible = false;
        }

        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
        currentPlace = null;
    }
}
