using System.Text.Json;
using LugarLang.Mobile.Models.Discovery;

namespace LugarLang.Mobile.Services.Content;

public class CategoryContentService
{
    private readonly string filePath;

    private List<DiscoveryCategory> categories =
        new();

    public string ExportToJson()
    {
        return JsonSerializer.Serialize(
            categories,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    public CategoryContentService()
    {


        filePath =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "discovery_categories.json");

        System.Diagnostics.Debug.WriteLine(
    $"CATEGORY CONTENT SERVICE FILE PATH: {filePath}");


        Load();
    }

    public List<DiscoveryCategory> GetAllCategories()
    {
        return categories
            .OrderBy(
                category =>
                    category.DisplayOrder)
            .ToList();
    }

    public void AddCategory(
        DiscoveryCategory category)
    {
        categories.Add(category);

        Save();
    }

    public void UpdateCategory(
        DiscoveryCategory category)
    {
        DiscoveryCategory? existing =
            categories.FirstOrDefault(
                item =>
                    item.Id == category.Id);

        if (existing == null)
        {
            return;
        }

        existing.Name =
            category.Name;

        existing.Icon =
            category.Icon;

        existing.DisplayOrder =
            category.DisplayOrder;

        existing.IsEnabled =
            category.IsEnabled;

        Save();
    }

    public void RemoveCategory(
        DiscoveryCategory category)
    {
        categories.RemoveAll(
            item =>
                item.Id == category.Id);

        Save();
    }

    private void Load()
    {
        if (!File.Exists(filePath))
        {
            LoadDefaultCategories();

            Save();

            return;
        }

        string json =
            File.ReadAllText(filePath);

        categories =
            JsonSerializer.Deserialize<
                List<DiscoveryCategory>>(
                    json)
                ?? new List<DiscoveryCategory>();
    }

    private void Save()
    {
        string json =
            JsonSerializer.Serialize(
                categories,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            filePath,
            json);
    }

    private void LoadDefaultCategories()
    {
        try
        {
            using Stream stream =
                FileSystem.OpenAppPackageFileAsync(
                    "seed_categories.json").Result;

            using StreamReader reader =
                new StreamReader(stream);

            string json =
                reader.ReadToEnd();

            List<DiscoveryCategory>? seeded =
                JsonSerializer.Deserialize<List<DiscoveryCategory>>(
                    json);

            if (seeded != null)
            {
                categories = seeded;

                return;
            }
        }
        catch
        {
            // fall through to hardcoded fallback below
        }

        categories.Add(
            new DiscoveryCategory
            {
                Id = "food",
                Name = "Food",
                Icon = "🍽",
                DisplayOrder = 1,
                IsEnabled = true
            });
    }

    public void Reload()
    {
        categories =
            new List<DiscoveryCategory>();

        Load();
    }
}