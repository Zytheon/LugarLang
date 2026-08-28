using LugarLang.Mobile.Models.Discovery;
using LugarLang.Mobile.Services.Content;
using LugarLang.Mobile.Models;

namespace LugarLang.Mobile.Pages;

public partial class EditCategoryPage : ContentPage
{
    private readonly DiscoveryCategory category;

    private readonly CategoryContentService
        categoryContentService;
    private readonly PlaceContentService
    placeContentService;

    public EditCategoryPage(
        DiscoveryCategory category,
        CategoryContentService categoryContentService)
    {
        InitializeComponent();

        this.category =
            category;

        this.categoryContentService =
            categoryContentService;

        placeContentService =
            new PlaceContentService();

        Entry? nameEntry =
            this.FindByName<Entry>(
                "NameEntry");

        Entry? iconEntry =
            this.FindByName<Entry>(
                "IconEntry");

        if (nameEntry != null)
        {
            nameEntry.Text =
                category.Name;
        }

        if (iconEntry != null)
        {
            iconEntry.Text =
                category.Icon;
        }

        DisplayOrderEntry.Text =
            category.DisplayOrder.ToString();

        EnabledSwitch.IsToggled =
            category.IsEnabled;

        RefreshPlaces();
    }

    private void RefreshPlaces()
    {
        List<Place> places =
            placeContentService
                .GetAllPlaces()
                .Where(
                    place =>
                        place.CategoryId == category.Id)
                .ToList();

        foreach (Place place in places)
        {
            System.Diagnostics.Debug.WriteLine(
                $"EDIT CATEGORY TILE: " +
                $"Name={place.Name}, " +
                $"CategoryId={place.CategoryId}, " +
                $"Category={place.Category}");
        }

        PlaceCollectionView.ItemsSource =
            places;
    }

    private async void OnEditPlaceClicked(
    object sender,
    EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.BindingContext
            is not Place place)
        {
            return;
        }

        await Navigation.PushAsync(
            new PlaceEditPage(
                place,
                placeContentService));
    }


    private async void OnDeletePlaceClicked(
    object sender,
    EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.BindingContext
            is not Place place)
        {
            return;
        }

        bool confirm =
            await DisplayAlertAsync(
                "Delete Tile",
                $"Are you sure you want to permanently delete \"{place.Name}\"?",
                "Delete",
                "Cancel");

        if (!confirm)
        {
            return;
        }

        placeContentService.RemovePlace(
            place);

        RefreshPlaces();
    }

    private async void OnAddPlaceClicked(
    object sender,
    EventArgs e)
    {
        Place place =
            new Place
            {
                Category =
                    category.Name,

                CategoryId =
                    category.Id,

                Region =
                    string.Empty
            };

        placeContentService.AddPlace(
            place);

        await Navigation.PushAsync(
            new PlaceEditPage(
                place,
                placeContentService));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        RefreshPlaces();
    }

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        string name =
            NameEntry.Text?.Trim() ??
            string.Empty;

        string icon =
            IconEntry.Text?.Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync(
                "Missing name",
                "Please enter a category name.",
                "OK");

            return;
        }

        if (!int.TryParse(
                DisplayOrderEntry.Text,
                out int displayOrder))
        {
            await DisplayAlertAsync(
                "Invalid display order",
                "Please enter a whole number.",
                "OK");

            return;
        }

        category.Name =
            name;

        category.Icon =
            icon;

        category.DisplayOrder =
            displayOrder;

        category.IsEnabled =
            EnabledSwitch.IsToggled;

        categoryContentService.UpdateCategory(
            category);

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


}