using System.Text.Json;
using LugarLang.Mobile.Models.Developer;
using Microsoft.Maui.Controls;

namespace LugarLang.Mobile.Services.Developer;

public class DeveloperLayoutPersistenceService
{
    private readonly DeveloperElementPathService
        elementPathService =
            new();

    private readonly List<DeveloperLayoutEntry>
        entries =
            new();

    private readonly string filePath;

    public DeveloperLayoutPersistenceService()
    {
        filePath =
            Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "developer-layout.json");

        Load();
    }

    public void ApplyToPage(
    string pageKey,
    Element root)
    {
        System.Diagnostics.Debug.WriteLine(
            $"DEVELOPER APPLY: {pageKey}");

        foreach (
            DeveloperLayoutEntry entry
            in entries.Where(
                x => x.PageKey == pageKey))
        {
            System.Diagnostics.Debug.WriteLine(
                $"DEVELOPER APPLY ELEMENT: " +
                $"{entry.ElementPath} " +
                $"X={entry.TranslationX} " +
                $"Y={entry.TranslationY}");

            VisualElement? element =
                elementPathService.FindByPath(
                    root,
                    entry.ElementPath);

            if (element == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "DEVELOPER APPLY FAILED: " +
                    "element not found.");

                continue;
            }

            element.TranslationX =
                entry.TranslationX;

            element.TranslationY =
                entry.TranslationY;

            if (entry.Width.HasValue)
            {
                element.WidthRequest =
                    entry.Width.Value;
            }

            if (entry.Height.HasValue)
            {
                element.HeightRequest =
                    entry.Height.Value;
            }
        }
    }

    public void SetEntry(
        string pageKey,
        string elementPath,
        double translationX,
        double translationY,
        double? width = null,
        double? height = null)
    {
        DeveloperLayoutEntry? existing =
            entries.FirstOrDefault(
                entry =>
                    entry.PageKey == pageKey &&
                    entry.ElementPath == elementPath);

        if (existing == null)
        {
            entries.Add(
                new DeveloperLayoutEntry
                {
                    PageKey = pageKey,
                    ElementPath = elementPath,
                    TranslationX = translationX,
                    TranslationY = translationY,
                    Width = width,
                    Height = height
                });

            return;
        }

        existing.TranslationX =
            translationX;

        existing.TranslationY =
            translationY;

        if (width.HasValue)
        {
            existing.Width =
                width;
        }

        if (height.HasValue)
        {
            existing.Height =
                height;
        }
    }

    public void WriteToDisk()
    {
        Save();
    }

    private void Load()
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            string json =
                File.ReadAllText(filePath);

            List<DeveloperLayoutEntry>? loaded =
                JsonSerializer.Deserialize<
                    List<DeveloperLayoutEntry>>(
                    json);

            if (loaded == null)
            {
                return;
            }

            entries.Clear();
            entries.AddRange(loaded);
        }
        catch
        {
            entries.Clear();
        }
    }

    private void Save()
    {
        string json =
            JsonSerializer.Serialize(
                entries,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            filePath,
            json);

        System.Diagnostics.Debug.WriteLine(
            $"DEVELOPER COMMIT: {filePath}");

        System.Diagnostics.Debug.WriteLine(
            json);
    }
}