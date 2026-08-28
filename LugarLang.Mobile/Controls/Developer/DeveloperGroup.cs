namespace LugarLang.Mobile.Controls.Developer;

public static class DeveloperGroup
{
    public static readonly BindableProperty GroupIdProperty =
        BindableProperty.CreateAttached(
            "GroupId",
            typeof(string),
            typeof(DeveloperGroup),
            null);

    public static readonly BindableProperty GroupNameProperty =
        BindableProperty.CreateAttached(
            "GroupName",
            typeof(string),
            typeof(DeveloperGroup),
            null);

    public static string? GetGroupId(
        BindableObject view)
    {
        return (string?)view.GetValue(
            GroupIdProperty);
    }

    public static void SetGroupId(
        BindableObject view,
        string? value)
    {
        view.SetValue(
            GroupIdProperty,
            value);
    }

    public static string? GetGroupName(
        BindableObject view)
    {
        return (string?)view.GetValue(
            GroupNameProperty);
    }

    public static void SetGroupName(
        BindableObject view,
        string? value)
    {
        view.SetValue(
            GroupNameProperty,
            value);
    }
}