using LugarLang.Mobile.Models;
using LugarLang.Mobile.Services;

namespace LugarLang.Mobile;

public partial class MainPage : ContentPage
{
    private readonly AutocompleteService autocompleteService;
    private readonly RouteSearchService routeSearchService;
    private readonly TransferRouteSearchService transferRouteSearchService;

    public MainPage()
    {
        InitializeComponent();

        TransitDataService transitDataService = new();

        autocompleteService = new AutocompleteService(
            transitDataService.Network);

        routeSearchService = new RouteSearchService(
            transitDataService.Network);

        transferRouteSearchService =
            new TransferRouteSearchService(
                transitDataService.Network);
    }

    private void FromEntry_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        string query = e.NewTextValue ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            FromSuggestions.ItemsSource = null;
            return;
        }

        FromSuggestions.ItemsSource =
            autocompleteService.Search(query);
    }

    private void FromSuggestions_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        string selected =
            e.CurrentSelection[0]?.ToString() ?? "";

        FromEntry.Text = selected;
        FromSuggestions.ItemsSource = null;
    }

    private void ToEntry_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        string query = e.NewTextValue ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            ToSuggestions.ItemsSource = null;
            return;
        }

        ToSuggestions.ItemsSource =
            autocompleteService.Search(query);
    }

    private void ToSuggestions_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        string selected =
            e.CurrentSelection[0]?.ToString() ?? "";

        ToEntry.Text = selected;
        ToSuggestions.ItemsSource = null;
    }

    private void FindRouteButton_Clicked(
        object? sender,
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

        // Try direct route first
        RouteSearchResult directResult =
            routeSearchService.Search(from, to);

        if (directResult.Found)
        {
            DisplayDirectRoute(directResult);
            return;
        }

        // If no direct route, try one transfer
        TransferRouteSearchResult transferResult =
            transferRouteSearchService.Search(from, to);

        if (transferResult.Found)
        {
            DisplayTransferRoute(transferResult);
            return;
        }

        ResultsLabel.Text =
            "No route found.";
    }

    private void DisplayDirectRoute(
        RouteSearchResult result)
    {
        string output =
            $"🚍 Route: {result.RouteId}\n\n" +
            $"🧭 Direction: {result.Direction}\n\n" +
            $"📍 Board:\n{result.BoardingStop?.Name}\n\n";

        output += "🛑 Stops Along Your Journey\n\n";

        int number = 1;

        foreach (var stop in result.Stops)
        {
            output += $"{number}. {stop.Name}\n";
            number++;
        }

        output +=
            $"\n⬇️ Get Off:\n{result.DestinationStop?.Name}";

        ResultsLabel.Text = output;
    }

    private void DisplayTransferRoute(
        TransferRouteSearchResult result)
    {
        string output =
            "🔄 ONE TRANSFER JOURNEY\n\n";

        output +=
            $"🚍 First Ride: {result.FirstRide.RouteId}\n\n";

        output +=
            $"📍 Board:\n{result.FirstRide.BoardingStop?.Name}\n\n";

        output +=
            $"⬇️ Get Off:\n{result.TransferStop?.Name}\n\n";

        output +=
            "══════════════════════\n";
        output +=
            "🔄 TRANSFER HERE\n";
        output +=
            "══════════════════════\n\n";

        output +=
            $"🚍 Second Ride: {result.SecondRide.RouteId}\n\n";

        output +=
            $"📍 Board:\n{result.TransferStop?.Name}\n\n";

        output +=
            $"⬇️ Get Off:\n{result.SecondRide.DestinationStop?.Name}";

        ResultsLabel.Text = output;
    }
}
