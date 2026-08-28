using LugarLang.Mobile.Models.Discovery;
using LugarLang.Mobile.Services.Content;

namespace LugarLang.Mobile.Pages;

public partial class CategoryManagementPage : ContentPage
{
    private readonly CategoryContentService
        categoryContentService;

    public CategoryManagementPage()
    {
        InitializeComponent();

        categoryContentService =
            new CategoryContentService();

        RefreshCategories();
    }

    private void RefreshCategories()
    {
        CategoryCollectionView.ItemsSource =
            categoryContentService
                .GetAllCategories()
                .ToList();

        foreach (
    DiscoveryCategory category
    in categoryContentService.GetAllCategories())
        {
            System.Diagnostics.Debug.WriteLine(
                $"CATEGORY: Id={category.Id}, Name={category.Name}, Enabled={category.IsEnabled}");
        }
    }

    private async void OnAddCategoryClicked(
    object sender,
    EventArgs e)
    {
        await Navigation.PushAsync(
            new AddCategoryPage(
                categoryContentService));
    }

    private async void OnEditCategoryClicked(
        object sender,
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
        object sender,
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

        RefreshCategories();
    }

}