using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperElementPathService
{
    public string GetPath(
        Element root,
        VisualElement target)
    {
        if (ReferenceEquals(root, target))
        {
            return string.Empty;
        }

        List<int> indices = new();

        IVisualTreeElement? current = target;

        while (
            current != null &&
            !ReferenceEquals(current, root))
        {
            IVisualTreeElement? parent =
                current.GetVisualParent();

            if (parent == null)
            {
                return string.Empty;
            }

            IReadOnlyList<IVisualTreeElement> children =
                parent.GetVisualChildren();

            int index = -1;

            for (int i = 0; i < children.Count; i++)
            {
                if (ReferenceEquals(
                        children[i],
                        current))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return string.Empty;
            }

            indices.Add(index);

            current = parent;
        }

        indices.Reverse();

        return string.Join("/", indices);
    }

    public VisualElement? FindByPath(
        Element root,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return root as VisualElement;
        }

        IVisualTreeElement current = root;

        string[] parts =
            path.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int index))
            {
                return null;
            }

            IReadOnlyList<IVisualTreeElement> children =
                current.GetVisualChildren();

            if (index < 0 ||
                index >= children.Count)
            {
                return null;
            }

            current = children[index];
        }

        return current as VisualElement;
    }
}