using LugarLang.Mobile.Models.Developer;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperOverlayManager
{
    private readonly DeveloperLayoutPersistenceService
        persistenceService;

    private readonly DeveloperElementPathService
        elementPathService =
            new();

    public bool IsEnabled { get; set; } =
        true;

    public DeveloperOverlayManager(
        DeveloperLayoutPersistenceService
            persistenceService)
    {
        this.persistenceService =
            persistenceService;
    }

    public string GetPageKey(
        ContentPage page)
    {
        return page.GetType().FullName ??
               page.GetType().Name;
    }

    public void RecordLayoutChange(
        ContentPage page,
        Element root,
        VisualElement element,
        double translationX,
        double translationY,
        double? width = null,
        double? height = null)
    {

        System.Diagnostics.Debug.WriteLine(
    $"RECORD LAYOUT CHANGE: {element.GetType().Name} " +
    $"X={translationX} Y={translationY} W={width} H={height}");

        string pageKey =
            GetPageKey(page);

        string elementPath =
            elementPathService.GetPath(
                root,
                element);

        if (string.IsNullOrWhiteSpace(
                elementPath))
        {
            return;
        }

        persistenceService.SetEntry(
            pageKey,
            elementPath,
            translationX,
            translationY,
            width,
            height);
    }

    public Task CommitAsync()
    {
        persistenceService.WriteToDisk();

        return Task.CompletedTask;
    }

    public void RestoreLayout(
        ContentPage page,
        Element root)
    {
        string pageKey =
            GetPageKey(page);

        persistenceService.ApplyToPage(
            pageKey,
            root);
    }
}