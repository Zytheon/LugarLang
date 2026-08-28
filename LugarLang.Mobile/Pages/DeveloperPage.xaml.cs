namespace LugarLang.Mobile.Pages;

public partial class DeveloperPage : ContentPage
{
    public DeveloperPage()
    {
        InitializeComponent();
    }

    private async void OnManageCategoriesClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new CategoryManagementPage());
    }
}