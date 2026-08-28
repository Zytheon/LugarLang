namespace LugarLang.Mobile.Models.Discovery;

public class DiscoveryCategory
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString();

    public string Name { get; set; } =
        string.Empty;

    public string Icon { get; set; } =
        string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } =
        true;
}