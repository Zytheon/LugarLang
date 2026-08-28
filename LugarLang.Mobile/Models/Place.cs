namespace LugarLang.Mobile.Models;

public class Place
{
    public string Name { get; set; } = "";

    public string Category { get; set; } = "";
    public string CategoryId { get; set; } = "";

    public string Region { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public int FeaturedOrder { get; set; }
    public List<string> Photos { get; set; } =
        new();

    public PlaceContacts Contacts { get; set; } =
        new();

    public PlacePayments Payments { get; set; } =
        new();

    public string Description { get; set; } =
        string.Empty;
}