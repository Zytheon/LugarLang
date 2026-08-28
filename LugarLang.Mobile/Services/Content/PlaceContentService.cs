using System.Text.Json;
using LugarLang.Mobile.Models;

namespace LugarLang.Mobile.Services.Content;

public class PlaceContentService
{
    private readonly string filePath;

    private List<Place> places = new();

    public PlaceContentService()
    {
        filePath =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "places.json");

        Load();

        MigrateCategoryIds();

        if (places.Count == 0)
        {
            LoadDefaultPlaces();

            Save();
        }



    }

    private void MigrateCategoryIds()
    {
        bool changed = false;

        foreach (Place place in places)
        {
            if (!string.IsNullOrWhiteSpace(place.CategoryId))
            {
                continue;
            }

            if (
                string.Equals(
                    place.Category,
                    "Food",
                    StringComparison.OrdinalIgnoreCase))
            {
                place.CategoryId = "food";
                changed = true;
            }
        }

        if (changed)
        {
            Save();
        }
    }

    public List<Place> GetAllPlaces()
    {
        return places;
    }

    public Place? GetPlace(
        string name)
    {
        return places.FirstOrDefault(
            place =>
                place.Name == name);
    }

    public void AddPlace(
        Place place)
    {
        places.Add(place);

        Save();
    }

    public void UpdatePlace(
        Place place)
    {
        Save();
    }

    public void RemovePlace(
        Place place)
    {
        places.Remove(place);

        Save();
    }

    private void Save()
    {
        string json =
            JsonSerializer.Serialize(
                places,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            filePath,
            json);
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
                File.ReadAllText(
                    filePath);

            List<Place>? loadedPlaces =
                JsonSerializer.Deserialize<List<Place>>(
                    json);

            if (loadedPlaces != null)
            {
                places =
                    loadedPlaces;
            }
        }
        catch
        {
            places = new List<Place>();
        }
    }

    private void LoadDefaultPlaces()
    {
        places.Add(
            new Place
            {
                Name = "Cucina Higala",
                Category = "Food",
                CategoryId = "food",
                Region = "Northern Mindanao",
                Latitude = 8.477,
                Longitude = 124.646,
                IsFeatured = true,
                FeaturedOrder = 1
            });

        places.Add(
            new Place
            {
                Name = "Redtail Shrimps & More",
                Category = "Food",
                CategoryId = "food",
                Region = "Northern Mindanao",
                Latitude = 8.482,
                Longitude = 124.647,
                IsFeatured = true,
                FeaturedOrder = 2
            });

        places.Add(
            new Place
            {
                Name = "Bigby's Cafe and Restaurant",
                Category = "Food",
                CategoryId = "food",
                Region = "Northern Mindanao",
                Latitude = 8.485,
                Longitude = 124.654,
                IsFeatured = true,
                FeaturedOrder = 3
            });

        places.Add(
            new Place
            {
                Name = "Fat Chef",
                Category = "Food",
                CategoryId = "food",
                Region = "Northern Mindanao",
                Latitude = 8.482,
                Longitude = 124.649,
                IsFeatured = true,
                FeaturedOrder = 4
            });
    }

    public void Reload()
    {
        places =
            new List<Place>();

        Load();
    }

}