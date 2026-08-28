using LugarLang.Mobile.Controls.Developer;
using LugarLang.Mobile.Models.Developer;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperOverlayManager
{
    private readonly Dictionary<
        ContentPage,
        View> originalContents =
        new();

    private readonly Dictionary<
        ContentPage,
        Dictionary<
            VisualElement,
            DeveloperLayoutEntry>>
        workingChanges =
        new();

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

   

    private void AttachVisualLayers(
    ContentPage page,
    View originalContent)
    {

        if (!ReferenceEquals(
        page.Content,
        originalContent))
        {
            return;
        }

        Grid root =
            new Grid();

        DeveloperOverlay overlay =
            new DeveloperOverlay();

        overlay.SetEditableRoot(
            originalContent);

        overlay.SetDeveloperMode(
            IsEnabled);

        overlay.LayoutChanged =
            (element,
             translationX,
             translationY) =>
                OnLayoutChanged(
                    page,
                    originalContent,
                    element,
                    translationX,
                    translationY);

        overlay.ChangesDiscarded =
            () =>
                DiscardPageChanges(
                    page);

        DeveloperLauncher launcher =
            new DeveloperLauncher();

        launcher.EditUIClicked =
            overlay.BeginEditMode;

        launcher.CommitRequested =
            CommitAllAsync;

        // Detach from ContentPage FIRST.
        page.Content =
            null;

        // NOW the ScrollView is no longer owned by the page.
        root.Children.Add(
            originalContent);

        root.Children.Add(
            overlay);

        root.Children.Add(
            launcher);

        // Finally restore the page's content.
        page.Content =
            root;


    }

    private void OnLayoutChanged(
        ContentPage page,
        View root,
        VisualElement element,
        double translationX,
        double translationY)
    {
        if (!workingChanges.TryGetValue(
                page,
                out Dictionary<
                    VisualElement,
                    DeveloperLayoutEntry>?
                    pageChanges))
        {
            return;
        }

        string pageKey =
            page.GetType().FullName ??
            page.GetType().Name;

        string elementPath =
            elementPathService.GetPath(
                root,
                element);

        if (string.IsNullOrWhiteSpace(
                elementPath))
        {
            return;
        }

        pageChanges[element] =
            new DeveloperLayoutEntry
            {
                PageKey =
                    pageKey,

                ElementPath =
                    elementPath,

                TranslationX =
                    translationX,

                TranslationY =
                    translationY
            };
    }

    public Task CommitAllAsync()
    {
        foreach (
            KeyValuePair<
                ContentPage,
                Dictionary<
                    VisualElement,
                    DeveloperLayoutEntry>>
            pageEntry
            in workingChanges)
        {
            foreach (
                DeveloperLayoutEntry entry
                in pageEntry.Value.Values)
            {
                persistenceService.SetEntry(
                    entry.PageKey,
                    entry.ElementPath,
                    entry.TranslationX,
                    entry.TranslationY);
            }
        }

        persistenceService.WriteToDisk();

        return Task.CompletedTask;
    }

    public void DiscardPageChanges(
        ContentPage page)
    {
        if (workingChanges.TryGetValue(
                page,
                out Dictionary<
                    VisualElement,
                    DeveloperLayoutEntry>?
                    pageChanges))
        {
            pageChanges.Clear();
        }
    }

    public void RemoveFromPage(
        ContentPage page)
    {
        if (!originalContents.TryGetValue(
                page,
                out View? originalContent))
        {
            return;
        }

        page.Content =
            originalContent;

        originalContents.Remove(
            page);

        workingChanges.Remove(
            page);
    }
}