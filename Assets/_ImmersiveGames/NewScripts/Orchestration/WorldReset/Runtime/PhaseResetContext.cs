using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime
{
    public readonly struct PhaseResetContext
    {
        public PhaseResetContext(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            LevelContextSignature levelSignature,
            string resetSignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            LevelSignature = levelSignature;
            ResetSignature = string.IsNullOrWhiteSpace(resetSignature) ? string.Empty : resetSignature.Trim();
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public LevelContextSignature LevelSignature { get; }
        public string ResetSignature { get; }
        public bool IsValid => PhaseDefinitionRef != null && MacroRouteId.IsValid && LevelSignature.IsValid;
    }
}
