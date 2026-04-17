using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Core.Events;

namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Runtime
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

    public readonly struct PhaseResetCompletedEvent : IEvent
    {
        public PhaseResetCompletedEvent(PhaseResetContext resetContext, string reason, string source)
        {
            ResetContext = resetContext;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseResetContext ResetContext { get; }
        public string Reason { get; }
        public string Source { get; }

        public bool IsValid => ResetContext.IsValid;
    }
}

