using CdoGtfsConverter.Models;
using LugarLang.Mobile.Services;

namespace LugarLang.Mobile.Pages;

public partial class DiagnosticsPage : ContentPage
{
    private readonly TransportNetwork network;
    private readonly TransferDiagnosticsService transferDiagnosticsService;


    public DiagnosticsPage()
    {
        InitializeComponent();

        TransitDataService transitDataService = new();

        network = transitDataService.Network;

        transferDiagnosticsService =
            new TransferDiagnosticsService(network);

        ModePicker.SelectedIndex = 0;
    }



    private void ModePicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        ResultsLabel.Text = "";

        StopSearchEntry.Placeholder =
            ModePicker.SelectedIndex switch
            {
                0 => "Search stop...",
                1 => "Search transfer stop...",
                _ => "Search..."
            };
    }


    private void AnalyzeButton_Clicked(
        object? sender,
        EventArgs e)
    {
        string query =
            StopSearchEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            ResultsLabel.Text =
                "Enter a search.";

            return;
        }

        switch (ModePicker.SelectedIndex)
        {
            case 0:
                SearchRoutesAtStop(query);
                break;

            case 1:
                SearchTransferOpportunities(query);
                break;
        }
    }




    private void SearchRoutesAtStop(
        string query)
    {
        string output =
            $"🔎 Results for: {query}\n\n";

        int routeCount = 0;

        foreach (Route route in network.Routes)
        {
            if (route.Inbound != null)
            {
                bool found =
                    route.Inbound.Stops.Any(
                        stop => stop.Name.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase));

                if (found)
                {
                    routeCount++;

                    output +=
                        $"🚍 Route: {route.Id}\n" +
                        $"Direction: Inbound\n\n" +
                        "Stops:\n";

                    int number = 1;

                    foreach (var stop in route.Inbound.Stops)
                    {
                        output +=
                            $"{number}. {stop.Name}\n";

                        number++;
                    }

                    output += "\n";
                }
            }

            if (route.Outbound != null)
            {
                bool found =
                    route.Outbound.Stops.Any(
                        stop => stop.Name.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase));

                if (found)
                {
                    routeCount++;

                    output +=
                        $"🚍 Route: {route.Id}\n" +
                        $"Direction: Outbound\n\n" +
                        "Stops:\n";

                    int number = 1;

                    foreach (var stop in route.Outbound.Stops)
                    {
                        output +=
                            $"{number}. {stop.Name}\n";

                        number++;
                    }

                    output += "\n";
                }
            }
        }

        if (routeCount == 0)
        {
            output += "No routes found.";
        }
        else
        {
            output +=
                $"Total matches: {routeCount}";
        }

        ResultsLabel.Text = output;
    }



    private void SearchTransferOpportunities(
        string query)
    {
        var routes =
            transferDiagnosticsService.FindRoutesAtStop(query);

        string output =
            $"🔄 Transfer Opportunities at {query}\n\n";

        if (routes.Count == 0)
        {
            output +=
                "No routes serve this stop.";

            ResultsLabel.Text = output;
            return;
        }

        output +=
            "Routes serving this stop:\n\n";

        foreach (var route in routes)
        {
            output +=
                $"🚍 {route.RouteId} ({route.Direction})\n";
        }

        output +=
            "\nPossible Transfers\n\n";

        bool foundTransfer = false;

        for (int i = 0; i < routes.Count; i++)
        {
            for (int j = i + 1; j < routes.Count; j++)
            {
                output +=
                    $"🔄 {routes[i].RouteId} ↔ {routes[j].RouteId}\n";

                foundTransfer = true;
            }
        }

        if (!foundTransfer)
        {
            output +=
                "No transfers available.";
        }

        ResultsLabel.Text = output;
    }
}
