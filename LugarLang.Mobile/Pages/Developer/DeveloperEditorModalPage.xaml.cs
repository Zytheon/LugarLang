namespace LugarLang.Mobile.Pages.Developer;

public enum DeveloperEditorLaunchMode
{
    Normal,
    MultiSelect,
    AddElement
}

public partial class DeveloperEditorModalPage : ContentPage
{
    public DeveloperEditorModalPage(
    View editableRoot,
    double verticalOffset,
    DeveloperEditorLaunchMode launchMode,
    Action<VisualElement, double, double>? onLayoutChanged = null,
    Action<VisualElement, double, double>? onSizeChanged = null)
    {
        InitializeComponent();

        EditorOverlay.SetEditableRoot(
            editableRoot);

        EditorOverlay.SetVerticalOffset(
            verticalOffset);

        EditorOverlay.DeveloperLayoutChanged =
            onLayoutChanged;

        EditorOverlay.DeveloperSizeChanged =
            onSizeChanged;

        EditorOverlay.EditingCompleted =
            async () => await CloseModal();

        EditorOverlay.ChangesDiscarded =
            async () => await CloseModal();

        switch (launchMode)
        {
            case DeveloperEditorLaunchMode.MultiSelect:
                EditorOverlay.BeginEditMode(
                    startInMultiSelectMode: true);
                break;

            case DeveloperEditorLaunchMode.AddElement:
                EditorOverlay.BeginAddElementMode();
                break;

            default:
                EditorOverlay.BeginEditMode();
                break;
        }
    }

    private async Task CloseModal()
    {

        System.Diagnostics.Debug.WriteLine(
    "CLOSE MODAL CALLED — stack trace follows:");

        System.Diagnostics.Debug.WriteLine(
            Environment.StackTrace);

        await Navigation.PopModalAsync(
            animated: false);
    }
}