using LugarLang.Mobile.Services;

namespace LugarLang.Mobile;

public partial class MainPage : ContentPage
{
    private readonly AutocompleteService autocompleteService;
    private readonly RouteSearchService routeSearchService;


    public MainPage()
    {
        InitializeComponent();


        TransitDataService transitDataService = new();


        autocompleteService = new AutocompleteService(
            transitDataService.Network);


        routeSearchService = new RouteSearchService(
            transitDataService.Network);
    }



    private void FromEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string query = e.NewTextValue;


        if (string.IsNullOrWhiteSpace(query))
        {
            FromSuggestions.ItemsSource = null;
            return;
        }


        var results = autocompleteService.Search(query);


        FromSuggestions.ItemsSource = results;
    }



    private void FromSuggestions_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }


        string selected =
            e.CurrentSelection[0]?.ToString() ?? "";


        FromEntry.Text = selected;


        FromSuggestions.ItemsSource = null;
    }



    private void ToEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string query = e.NewTextValue;


        if (string.IsNullOrWhiteSpace(query))
        {
            ToSuggestions.ItemsSource = null;
            return;
        }


        var results = autocompleteService.Search(query);


        ToSuggestions.ItemsSource = results;
    }



    private void ToSuggestions_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }


        string selected =
            e.CurrentSelection[0]?.ToString() ?? "";


        ToEntry.Text = selected;


        ToSuggestions.ItemsSource = null;
    }



    private void FindRouteButton_Clicked(
        object sender,
        EventArgs e)
    {
        string from = FromEntry.Text ?? "";
        string to = ToEntry.Text ?? "";


        if (string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(to))
        {
            ResultsLabel.Text =
                "Please select both From and To locations.";

            return;
        }


        string result =
            routeSearchService.Search(from, to);


        ResultsLabel.Text = result;
    }
}
