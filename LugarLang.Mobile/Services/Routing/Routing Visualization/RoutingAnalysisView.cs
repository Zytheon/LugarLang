using Microsoft.Maui.Controls;
using LugarLang.Mobile.UI.Routing;

namespace LugarLang.Mobile.Services.RoutingVisualization;

public class RoutingAnalysisView : ContentView
{
    private readonly RoutingCandidateView candidateView;

    public event Action<RoutingDebugInfo>? CandidateSelected;

    public RoutingAnalysisView()
    {
        candidateView =
            new RoutingCandidateView();

        candidateView.CandidateSelected +=
            CandidateView_CandidateSelected;

        Content =
            candidateView;
    }

    public void Display(
        RoutingDebugSnapshot snapshot)
    {
    }

    private void CandidateView_CandidateSelected(
        object? sender,
        RoutingDebugInfo candidate)
    {
        CandidateSelected?.Invoke(
            candidate);
    }
}
