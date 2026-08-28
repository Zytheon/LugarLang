using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperEditableElementResolver
{
    public View Resolve(
        View element)
    {
        IVisualTreeElement? current =
            element;

        while (current != null)
        {
            if (current is View view)
            {
                string? group =
                    Controls.Developer
                        .DeveloperEditable
                        .GetEditableGroup(
                            view);

                if (!string.IsNullOrWhiteSpace(group))
                {
                    return view;
                }
            }

            current =
                current.GetVisualParent();
        }

        return element;
    }
}