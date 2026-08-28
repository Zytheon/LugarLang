namespace LugarLang.Mobile.Controls.Developer;



public partial class DeveloperEditorPopup : ContentView


{
    public DeveloperEditorPopup(
        View editableRoot,
        double width,
        double height,
        double verticalOffset)
    {
        InitializeComponent();

        IsVisible = true;
        System.Diagnostics.Debug.WriteLine(
"DeveloperEditorPopup is called");

        WidthRequest =
            width;

        HeightRequest =
            height;

        EditorOverlay.SetEditableRoot(
            editableRoot);

        EditorOverlay.SetVerticalOffset(
            verticalOffset);

        EditorOverlay.BeginEditMode();
    }
}