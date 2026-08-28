using LugarLang.Mobile.Models;
using LugarLang.Mobile.Services.Content;

namespace LugarLang.Mobile.Services;

public class RestaurantDiscoveryService
{
    private readonly PlaceContentService placeContentService;

    public RestaurantDiscoveryService(
        PlaceContentService placeContentService)
    {
        this.placeContentService =
            placeContentService;
    }

    public List<Place> GetFoodSuggestions(
        string region)
    {

        return placeContentService
            .GetAllPlaces()
            .Where(
                place =>
                    place.Category == "Food" &&
                    place.Region == region &&
                    place.IsFeatured)
            .OrderBy(
                place =>
                    place.FeaturedOrder)
            .ToList();
    }

    public void AddPlace(
        Place place)
    {
        placeContentService.AddPlace(
            place);
    }

    public void RemovePlace(
        Place place)
    {
        placeContentService.RemovePlace(
            place);
    }
}