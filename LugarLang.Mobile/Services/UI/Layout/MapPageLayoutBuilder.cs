
namespace LugarLang.Mobile.Services.UI.Layout;

public class MapPageLayoutBuilder
{
    public Grid Build(
        Microsoft.Maui.Controls.View mapView,
        Microsoft.Maui.Controls.View routingCandidateView,
        Microsoft.Maui.Controls.View controls,
        double width)
    {
        if (width <= 700)
        {
            return BuildPhoneLayout(
                mapView,
                routingCandidateView,
                controls);
        }

        return BuildDesktopLayout(
            mapView,
            routingCandidateView,
            controls);
    }

    private Grid BuildDesktopLayout(
        Microsoft.Maui.Controls.View mapView,
        Microsoft.Maui.Controls.View routingCandidateView,
        Microsoft.Maui.Controls.View controls)
    {
        Grid mainContent = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(320))
            }
        };

        Grid.SetColumn(mapView, 0);
        Grid.SetColumn(routingCandidateView, 1);

        mainContent.Children.Add(mapView);
        mainContent.Children.Add(routingCandidateView);

        Grid layout = new()
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
        Microsoft.Maui.Controls.View mapView,
        Microsoft.Maui.Controls.View routingCandidateView,
        Microsoft.Maui.Controls.View controls)
    {
        phoneMapRow =
            new RowDefinition(
                new GridLength(1, GridUnitType.Star));

        phoneBottomRow =
            new RowDefinition(
                new GridLength(2, GridUnitType.Star));

        Grid mainContent = new()
        {
            RowDefinitions =
            {
                phoneMapRow,
                phoneBottomRow
            }
        };

        Grid.SetRow(mapView, 0);
        Grid.SetRow(routingCandidateView, 1);

        mainContent.Children.Add(mapView);
        mainContent.Children.Add(routingCandidateView);

        Grid layout = new()
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

    private RowDefinition? phoneMapRow;
    private RowDefinition? phoneBottomRow;

    public void SetDetailMode(bool detail)
    {
        if (phoneMapRow == null || phoneBottomRow == null)
            return;

        phoneMapRow.Height =
            new GridLength(
                detail ? 2 : 1,
                GridUnitType.Star);

        phoneBottomRow.Height =
            new GridLength(
                detail ? 3 : 2,
                GridUnitType.Star);
    }
}
