using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Controls.Developer;

public static class DeveloperEditable
{
    public static readonly BindableProperty
        EditableGroupProperty =
            BindableProperty.CreateAttached(
                "EditableGroup",
                typeof(string),
                typeof(DeveloperEditable),
                null);

    public static string? GetEditableGroup(
        BindableObject element)
    {
        return (string?)element.GetValue(
            EditableGroupProperty);
    }

    public static void SetEditableGroup(
        BindableObject element,
        string? value)
    {
        element.SetValue(
            EditableGroupProperty,
            value);
    }
}