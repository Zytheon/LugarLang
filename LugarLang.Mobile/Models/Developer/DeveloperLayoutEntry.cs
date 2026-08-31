namespace LugarLang.Mobile.Models.Developer;

public sealed class DeveloperLayoutEntry
{
    public string PageKey { get; set; } = string.Empty;

    public string ElementPath { get; set; } = string.Empty;

    public double TranslationX { get; set; }

    public double TranslationY { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}