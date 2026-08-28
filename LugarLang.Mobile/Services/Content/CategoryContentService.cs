using System.Text.Json;
using LugarLang.Mobile.Models.Discovery;

namespace LugarLang.Mobile.Services.Content;

public class CategoryContentService
{
    private readonly string filePath;

    private List<DiscoveryCategory> categories =
        new();

    public CategoryContentService()
    {
        filePath =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "discovery_categories.json");

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