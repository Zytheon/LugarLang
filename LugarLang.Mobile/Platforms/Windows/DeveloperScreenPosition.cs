#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace LugarLang.Mobile.Controls.Developer;

public static class DeveloperScreenPosition
{
    public static double GetTopOffset(
        Microsoft.Maui.Controls.VisualElement element)
    {
        if (element.Handler?.PlatformView is not FrameworkElement platformView)
        {
            return 0;
        }

        GeneralTransform transform =
            platformView.TransformToVisual(null);

        Windows.Foundation.Point screenPoint =
            transform.TransformPoint(
                new Windows.Foundation.Point(0, 0));

        return screenPoint.Y;
    }
}
#endif