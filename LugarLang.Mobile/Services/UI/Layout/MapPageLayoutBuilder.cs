using Mapsui.UI.Maui;

namespace LugarLang.Mobile.UI.Layout;

public class MapPageLayoutBuilder
{
    public Grid Build(
        MapControl mapControl,
        View routingCandidateView,
        Picker routePicker,
        Picker walkingDistancePicker,
        Button fromButton,
        Button toButton,
        double width)
    {
        if (width <= 700)
        {
            return BuildPhoneLayout(
                mapControl,
                routingCandidateView,
                routePicker,
                walkingDistancePicker,
                fromButton,
                toButton);
        }

        return BuildDesktopLayout(
            mapControl,
            routingCandidateView,
            routePicker,
            walkingDistancePicker,
            fromButton,
            toButton);
    }

    private Grid BuildDesktopLayout(
        MapControl mapControl,
        View routingCandidateView,
        Picker routePicker,
        Picker walkingDistancePicker,
        Button fromButton,
        Button toButton)
    {
        Grid controls =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

        Grid.SetColumn(routePicker, 0);
        Grid.SetColumn(walkingDistancePicker, 1);
        Grid.SetColumn(fromButton, 2);
        Grid.SetColumn(toButton, 3);

        controls.Children.Add(routePicker);
        controls.Children.Add(walkingDistancePicker);
        controls.Children.Add(fromButton);
        controls.Children.Add(toButton);

        Grid mainContent =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(new GridLength(320))
                }
            };

        Grid.SetColumn(mapControl, 0);
        Grid.SetColumn(routingCandidateView, 1);

        mainContent.Children.Add(mapControl);
        mainContent.Children.Add(routingCandidateView);

        Grid layout =
            new()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

        Grid.SetRow(controls, 0);
        Grid.SetRow(mainContent, 1);

        layout.Children.Add(controls);
        layout.Children.Add(mainContent);

        return layout;
    }

    private Grid BuildPhoneLayout(
        MapControl mapControl,
        View routingCandidateView,
        Picker routePicker,
        Picker walkingDistancePicker,
        Button fromButton,
        Button toButton)
    {
        Grid controls =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

        Grid.SetColumn(routePicker, 0);
        Grid.SetColumn(walkingDistancePicker, 1);
        Grid.SetColumn(fromButton, 2);
        Grid.SetColumn(toButton, 3);

        controls.Children.Add(routePicker);
        controls.Children.Add(walkingDistancePicker);
        controls.Children.Add(fromButton);
        controls.Children.Add(toButton);

        Grid mainContent =
            new()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(new GridLength(260))
                }
            };

        Grid.SetRow(mapControl, 0);
        Grid.SetRow(routingCandidateView, 1);

        mainContent.Children.Add(mapControl);
        mainContent.Children.Add(routingCandidateView);

        Grid layout =
            new()
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

        Grid.SetRow(controls, 0);
        Grid.SetRow(mainContent, 1);

        layout.Children.Add(controls);
        layout.Children.Add(mainContent);

        return layout;
    }
}



