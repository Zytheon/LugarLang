namespace LugarLang.Mobile.Pages.Developer;

public partial class DeveloperEditorModalPage : ContentPage
{
    public DeveloperEditorModalPage(
        View editableRoot,
        double verticalOffset,
        bool startInMultiSelectMode = false)
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

        EditorOverlay.BeginEditMode(
            startInMultiSelectMode);
    }

    private async Task CloseModal()
    {
        await Navigation.PopModalAsync(
            animated: false);
    }
}