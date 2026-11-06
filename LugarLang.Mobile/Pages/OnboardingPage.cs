using LugarLang.Mobile.UI.Settings;

namespace LugarLang.Mobile.Pages;

public class OnboardingPage : ContentPage
{
    private readonly View[] steps;
    private readonly BoxView[] dots;
    private readonly Button backButton;
    private readonly Button nextButton;
    private readonly CheckBox agreeCheck;
    private int stepIndex;

    public OnboardingPage()
    {
        BackgroundColor = UiColors.PageBackground;

        agreeCheck = new CheckBox { Color = UiColors.PrimaryBlue };
        agreeCheck.CheckedChanged += (_, _) => UpdateNavigation();

        steps = new View[]
        {
            CreateStep(
                "Welcome to LugarLang",
                "Discover places worth visiting and find your way " +
                "around by jeepney and on foot.",
                new Label
                {
                    Text = "📍",
                    FontSize = 84,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }),

            CreateStep(
                "Set a preferred language",
                "You can change this any time from the menu.",
                new LanguageView()),

            CreateStep(
                "Permissions and terms",
                "Allow location for places near you, then review the terms.",
                CreatePermissionsAndTerms())
        };

        Grid stepHost = new();
        foreach (View step in steps)
        {
            step.IsVisible = false;
            stepHost.Children.Add(step);
        }
        steps[0].IsVisible = true;

        backButton = new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = UiColors.TextSecondary
        };
        backButton.Clicked += (_, _) => GoTo(stepIndex - 1);

        nextButton = new Button
        {
            CornerRadius = 14,
            HeightRequest = 48,
            Padding = new Thickness(28, 0),
            BackgroundColor = UiColors.PrimaryBlue,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };
        nextButton.Clicked += OnNextClicked;

        dots = new BoxView[steps.Length];
        HorizontalStackLayout dotRow = new()
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i] = new BoxView
            {
                WidthRequest = 8,
                HeightRequest = 8,
                CornerRadius = 4
            };
            dotRow.Children.Add(dots[i]);
        }

        Grid bottomBar = new();
        bottomBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        bottomBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        bottomBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        bottomBar.Add(backButton, 0, 0);
        bottomBar.Add(dotRow, 1, 0);
        bottomBar.Add(nextButton, 2, 0);

        Grid root = new()
        {
            Padding = new Thickness(24, 48, 24, 24),
            RowSpacing = 20
        };
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.Add(stepHost, 0, 0);
        root.Add(bottomBar, 0, 1);

        Content = root;
        UpdateNavigation();
    }

    private static Grid CreateStep(string title, string subtitle, View body)
    {
        Grid step = new() { RowSpacing = 18 };
        step.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        step.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        step.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        step.Add(new Label
        {
            Text = title,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = UiColors.TextPrimary
        }, 0, 0);

        step.Add(new Label
        {
            Text = subtitle,
            FontSize = 14,
            TextColor = UiColors.TextSecondary
        }, 0, 1);

        step.Add(body, 0, 2);
        return step;
    }

    private View CreatePermissionsAndTerms()
    {
        Grid agree = new() { ColumnSpacing = 6 };
        agree.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        agree.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        agree.Add(agreeCheck, 0, 0);
        agree.Add(new Label
        {
            Text = "I have read and agree to the Terms of Service",
            FontSize = 13,
            TextColor = UiColors.TextPrimary,
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        Grid body = new() { RowSpacing = 14 };
        body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        body.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        body.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        body.Add(new PermissionsView(), 0, 0);
        body.Add(new TermsView(), 0, 1);
        body.Add(agree, 0, 2);
        return body;
    }

    private void GoTo(int index)
    {
        if (index < 0 || index >= steps.Length) return;

        steps[stepIndex].IsVisible = false;
        stepIndex = index;
        steps[stepIndex].IsVisible = true;
        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        bool last = stepIndex == steps.Length - 1;
        bool canProceed = !last || agreeCheck.IsChecked;

        backButton.IsEnabled = stepIndex > 0;
        backButton.Opacity = stepIndex > 0 ? 1 : 0;

        nextButton.Text = last ? "Get started" : "Next";
        nextButton.IsEnabled = canProceed;
        nextButton.Opacity = canProceed ? 1 : 0.5;

        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].Color = i == stepIndex
                ? UiColors.PrimaryBlue
                : UiColors.BorderBlue;
        }
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (stepIndex < steps.Length - 1)
        {
            GoTo(stepIndex + 1);
            return;
        }

        ((App)Microsoft.Maui.Controls.Application.Current!).FinishOnboarding();
    }

    protected override bool OnBackButtonPressed()
    {
        if (stepIndex > 0)
        {
            GoTo(stepIndex - 1);
            return true;
        }

        return base.OnBackButtonPressed();
    }
}