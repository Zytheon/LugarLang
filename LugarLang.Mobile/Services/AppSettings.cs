namespace LugarLang.Mobile.Services;

public static class AppSettings
{
    private const string OnboardingDoneKey = "onboarding.done";
    private const string LanguageKey = "app.language";

    // Paste your real survey link (Google Forms, etc.) here.
    public const string SurveyUrl = "";

    public static readonly (string Code, string Name)[] Languages =
    {
        ("en", "English"),
        ("fil", "Filipino"),
        ("ceb", "Cebuano")
    };

    public static bool OnboardingDone
    {
        get => Preferences.Default.Get(OnboardingDoneKey, false);
        set => Preferences.Default.Set(OnboardingDoneKey, value);
    }

    public static string Language
    {
        get => Preferences.Default.Get(LanguageKey, "en");
        set => Preferences.Default.Set(LanguageKey, value);
    }
}