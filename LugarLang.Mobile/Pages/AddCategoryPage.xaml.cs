using LugarLang.Mobile.Models.Discovery;
using LugarLang.Mobile.Services.Content;

namespace LugarLang.Mobile.Pages;

public partial class AddCategoryPage : ContentPage
{
    private readonly CategoryContentService
        categoryContentService;

    public AddCategoryPage(
        CategoryContentService categoryContentService)
    {
        InitializeComponent();

        this.categoryContentService =
            categoryContentService;
    }

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        Entry? nameEntry =
            this.FindByName<Entry>(
                "NameEntry");

        Entry? iconEntry =
            this.FindByName<Entry>(
                "IconEntry");

        Entry? displayOrderEntry =
            this.FindByName<Entry>(
                "DisplayOrderEntry");

        Switch? enabledSwitch =
            this.FindByName<Switch>(
                "EnabledSwitch");

        string name =
            nameEntry?.Text?.Trim() ??
            string.Empty;

        string icon =
            iconEntry?.Text?.Trim() ??
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
                displayOrderEntry?.Text,
                out int displayOrder))
        {
            await DisplayAlertAsync(
                "Invalid display order",
                "Please enter a whole number.",
                "OK");

            return;
        }

        DiscoveryCategory category =
            new DiscoveryCategory
            {
                Name = name,
                Icon = icon,
                DisplayOrder =
                    displayOrder,
                IsEnabled =
                    enabledSwitch?.IsToggled ??
                    true
            };

        categoryContentService.AddCategory(
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