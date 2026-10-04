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
        AppShell shell =
            Handler?
                .MauiContext?
                .Services
                .GetRequiredService<AppShell>()
            ?? throw new InvalidOperationException(
                "AppShell could not be resolved.");

        return new Window(shell);
    }
}
