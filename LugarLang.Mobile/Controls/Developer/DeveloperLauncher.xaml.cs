using LugarLang.Mobile.Controls.Developer;

namespace LugarLang.Mobile.Controls.Developer;

public partial class DeveloperLauncher : ContentView
{
    public Action? EditUIClicked
    {
        get;
        set;
    }



    public Action? MultiUIEditClicked
    {
        get;
        set;
    }

    public Func<Task>? CommitRequested
    {
        get;
        set;
    }

    public DeveloperLauncher()
    {
        InitializeComponent();

        PropertyChanged += DeveloperLauncher_PropertyChanged;

        DeveloperButton.PropertyChanged += DeveloperButton_PropertyChanged;
    }

    private void DeveloperLauncher_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible)
            && !IsVisible)
        {
            System.Diagnostics.Debug.WriteLine(
$"DeveloperLauncher PropertyChanged: {e.PropertyName}");
            System.Diagnostics.Debugger.Break();
        }
    }

    private void DeveloperButton_PropertyChanged(
    object? sender,
    System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible)
            && !DeveloperButton.IsVisible)
        {
            System.Diagnostics.Debugger.Break();
        }
    }

    private void OnDeveloperButtonClicked(
        object sender,
        EventArgs e)
    {
        Border? menu =
            this.FindByName<Border>(
                "DeveloperMenu");

        if (menu != null)
        {
            menu.IsVisible =
                !menu.IsVisible;
        }
    }

    private void OnMultiUIEditClicked(
    object sender,
    EventArgs e)
    {
        MultiUIEditClicked?.Invoke();

        CloseMenu();
    }

   

    private async void OnAddUIElementClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync(
            "Developer Tool",
            "Add UI Element will be implemented next.",
            "OK");
    }

    private void OnEditUIClicked(
        object sender,
        EventArgs e)
    {
        EditUIClicked?.Invoke();
        IsVisible = true;


        CloseMenu();
    }


    private async void OnBrandingClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync(
            "Developer Tool",
            "Branding editor will be implemented next.",
            "OK");
    }

    private async void OnMediaClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync(
            "Developer Tool",
            "Media editor will be implemented next.",
            "OK");
    }

    private async void OnCommitClicked(
        object sender,
        EventArgs e)
    {
        bool confirmed =
            await Shell.Current.DisplayAlertAsync(
                "Commit Changes",
                "This will make your current developer changes permanent. " +
                "They will remain after you close and reopen the app.\n\n" +
                "Do you want to commit these changes?",
                "Commit",
                "Cancel");

        if (!confirmed)
        {
            return;
        }

        if (CommitRequested != null)
        {
            await CommitRequested();
        }

        await Shell.Current.DisplayAlertAsync(
            "Changes Committed",
            "Your developer changes are now permanent.",
            "OK");

        CloseMenu();
    }

    private void CloseMenu()
    {
        Border? menu =
            this.FindByName<Border>(
                "DeveloperMenu");

        if (menu != null)
        {
            menu.IsVisible =
                false;
        }
    }
}