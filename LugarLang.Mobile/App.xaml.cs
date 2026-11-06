using LugarLang.Mobile.Pages;
using LugarLang.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LugarLang.Mobile;

public partial class App
    : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        Page startPage = AppSettings.OnboardingDone
            ? ResolveShell()
            : new OnboardingPage();

        return new Window(startPage);
    }

    public void FinishOnboarding()
    {
        AppSettings.OnboardingDone = true;

        if (Windows.Count > 0)
            Windows[0].Page = ResolveShell();
    }

    private AppShell ResolveShell() =>
        Handler?.MauiContext?.Services.GetRequiredService<AppShell>()
        ?? throw new InvalidOperationException(
            "AppShell could not be resolved.");
}
