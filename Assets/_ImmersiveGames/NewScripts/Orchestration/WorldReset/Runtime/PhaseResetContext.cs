using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime
{
    public readonly struct PhaseResetContext
    {
        public PhaseResetContext(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            PhaseContextSignature phaseSignature,
            string resetSignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            PhaseSignature = phaseSignature;
            ResetSignature = string.IsNullOrWhiteSpace(resetSignature) ? string.Empty : resetSignature.Trim();
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public PhaseContextSignature PhaseSignature { get; }
        public string ResetSignature { get; }
        public bool IsValid => PhaseDefinitionRef != null && MacroRouteId.IsValid && PhaseSignature.IsValid;
    }
}
