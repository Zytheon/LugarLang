using System.Text.Json;
using LugarLang.Mobile.Models;

namespace LugarLang.Mobile.Services.Content;

public class PlaceContentService
{
    private readonly string filePath;

    private List<Place> places = new();

    public string ExportToJson()
    {
        return JsonSerializer.Serialize(
            places,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    public PlaceContentService()
    {


        filePath =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "places.json");
        
        System.Diagnostics.Debug.WriteLine(
    $"PLACE CONTENT SERVICE FILE PATH: {filePath}");

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
        try
        {
            using Stream stream =
                FileSystem.OpenAppPackageFileAsync(
                    "seed_places.json").Result;

            using StreamReader reader =
                new StreamReader(stream);

            string json =
                reader.ReadToEnd();

            List<Place>? seeded =
                JsonSerializer.Deserialize<List<Place>>(
                    json);

            if (seeded != null)
            {
                places = seeded;

                return;
            }
        }
        catch
        {
            // fall through to hardcoded fallback below
        }

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
    }

    public void Reload()
    {
        places =
            new List<Place>();

        Load();
    }

}