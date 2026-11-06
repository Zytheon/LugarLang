using Microsoft.Maui.Controls.Shapes;

namespace LugarLang.Mobile.UI.Settings;

public class PermissionsView : ContentView
{
    private readonly Label statusLabel;
    private readonly Button actionButton;
    private bool openSettingsNext;
    private Window? hookedWindow;

    public PermissionsView()
    {
        statusLabel = new Label
        {
            FontSize = 12,
            TextColor = UiColors.TextSecondary
        };

        actionButton = new Button
        {
            Text = "Allow",
            HeightRequest = 40,
            CornerRadius = 12,
            Padding = new Thickness(20, 0),
            BackgroundColor = UiColors.PrimaryBlue,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        };
        actionButton.Clicked += OnActionClicked;

        VerticalStackLayout text = new()
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = "Location",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = UiColors.TextPrimary
                },
                new Label
                {
                    Text = "Used by Near Me to find places close to you.",
                    FontSize = 13,
                    TextColor = UiColors.TextSecondary
                },
                statusLabel
            }
        };

        Grid row = new() { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Add(text, 0, 0);
        row.Add(actionButton, 1, 0);

        Content = new Border
        {
            Padding = new Thickness(16),
            Stroke = UiColors.BorderBlue,
            StrokeThickness = 1,
            BackgroundColor = UiColors.SurfaceWhite,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = row
        };

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        hookedWindow = Window;
        if (hookedWindow != null)
            hookedWindow.Activated += OnWindowActivated;

        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (hookedWindow != null)
            hookedWindow.Activated -= OnWindowActivated;

        hookedWindow = null;
    }

    // Re-check when the user comes back from the system settings.
    private async void OnWindowActivated(object? sender, EventArgs e) =>
        await RefreshAsync();

    public async Task RefreshAsync()
    {
        PermissionStatus status =
            await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        bool granted = status == PermissionStatus.Granted;

        if (granted)
            openSettingsNext = false;

        statusLabel.Text = granted
            ? "Allowed"
            : status == PermissionStatus.Unknown
                ? "Not asked yet"
                : "Not allowed";

        actionButton.IsVisible = !granted;
        actionButton.Text = openSettingsNext ? "Open settings" : "Allow";
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        if (openSettingsNext)
        {
            AppInfo.ShowSettingsUI();
            return;
        }

        PermissionStatus status =
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        openSettingsNext = status != PermissionStatus.Granted;

        await RefreshAsync();
    }
}