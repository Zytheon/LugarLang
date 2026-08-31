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

        List<string> segments =
            new();

        IVisualTreeElement? current =
            target;

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

            string segment =
                current is VisualElement currentVisual &&
                !string.IsNullOrWhiteSpace(currentVisual.AutomationId)
                    ? $"id:{currentVisual.AutomationId}"
                    : $"idx:{index}";

            segments.Add(
                segment);

            current =
                parent;
        }

        segments.Reverse();

        return string.Join(
            "/",
            segments);
    }

    public VisualElement? FindByPath(
        Element root,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return root as VisualElement;
        }

        IVisualTreeElement current =
            root;

        string[] parts =
            path.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            IReadOnlyList<IVisualTreeElement> children =
                current.GetVisualChildren();

            IVisualTreeElement? next =
                null;

            if (part.StartsWith("id:"))
            {
                string targetId =
                    part.Substring(3);

                foreach (
                    IVisualTreeElement child
                    in children)
                {
                    if (child is VisualElement childVisual &&
                        childVisual.AutomationId == targetId)
                    {
                        next =
                            child;

                        break;
                    }
                }
            }
            else if (
                part.StartsWith("idx:") &&
                int.TryParse(
                    part.Substring(4),
                    out int index))
            {
                if (index >= 0 &&
                    index < children.Count)
                {
                    next =
                        children[index];
                }
            }

            if (next == null)
            {
                return null;
            }

            current =
                next;
        }

        return current as VisualElement;
    }
}