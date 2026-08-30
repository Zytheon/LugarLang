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
        DeveloperEditorLaunchMode launchMode = DeveloperEditorLaunchMode.Normal)
    {
        InitializeComponent();

        EditorOverlay.SetEditableRoot(
            editableRoot);

        EditorOverlay.SetVerticalOffset(
            verticalOffset);

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
        await Navigation.PopModalAsync(
            animated: false);
    }
}