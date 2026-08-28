using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperCoordinateMapper
{
    public Rect GetBoundsRelativeTo(
        VisualElement element,
        Element? root)
    {
        if (root == null)
        {
            return Rect.Zero;
        }

        double x = element.Bounds.X;
        double y = element.Bounds.Y;
        double width = element.Bounds.Width;
        double height = element.Bounds.Height;

        IVisualTreeElement? current = element;

        while (current != null)
        {
            IVisualTreeElement? parent = current.GetVisualParent();

            if (parent == null || parent == root)
            {
                break;
            }

            if (parent is VisualElement parentVisual)
            {
                x += parentVisual.Bounds.X;
                y += parentVisual.Bounds.Y;

                if (parentVisual is ScrollView scrollView)
                {
                    x -= scrollView.ScrollX;
                    y -= scrollView.ScrollY;
                }
            }

            current = parent;
        }

        return new Rect(x, y, width, height);
    }

    public Rect GetAbsoluteBounds(VisualElement element)
    {
        double x = element.Bounds.X;
        double y = element.Bounds.Y;
        double width = element.Bounds.Width;
        double height = element.Bounds.Height;

        IVisualTreeElement? current = element;

        while (current != null)
        {
            IVisualTreeElement? parent = current.GetVisualParent();

            if (parent == null)
            {
                break;
            }

            if (parent is VisualElement parentVisual)
            {
                x += parentVisual.Bounds.X;
                y += parentVisual.Bounds.Y;

                if (parentVisual is ScrollView scrollView)
                {
                    x -= scrollView.ScrollX;
                    y -= scrollView.ScrollY;
                }
            }

            current = parent;
        }

        return new Rect(x, y, width, height);
    }

    public Rect MapElementToOverlaySpace(
        VisualElement element,
        VisualElement overlay)
    {
        Rect overlayAbsolute =
            GetAbsoluteBounds(overlay);

        return MapElementToOverlaySpace(
            element,
            overlayAbsolute);
    }

    public Rect MapElementToOverlaySpace(
        VisualElement element,
        Rect overlayAbsoluteBounds)
    {
        Rect elementAbsolute =
            GetAbsoluteBounds(element);

        return new Rect(
            elementAbsolute.X - overlayAbsoluteBounds.X,
            elementAbsolute.Y - overlayAbsoluteBounds.Y,
            elementAbsolute.Width,
            elementAbsolute.Height);
    }
}