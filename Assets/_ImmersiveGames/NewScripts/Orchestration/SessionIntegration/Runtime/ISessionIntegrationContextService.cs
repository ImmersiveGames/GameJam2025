using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime
{
    public interface ISessionIntegrationContextService
    {
        IGameplaySessionContextService SessionContextService { get; }
        IGameplayPhaseRuntimeService PhaseRuntimeService { get; }
        IGameplayParticipationFlowService ParticipationService { get; }

        SessionIntegrationContextSnapshot Current { get; }
        bool TryGetCurrent(out SessionIntegrationContextSnapshot snapshot);
        bool TryGetCurrentSessionContext(out GameplaySessionContextSnapshot snapshot);
        bool TryGetCurrentPhaseRuntime(out GameplayPhaseRuntimeSnapshot snapshot);
        bool TryGetCurrentParticipation(out ParticipationSnapshot snapshot);
        void RequestGameplayInputMode(string reason, string semanticSource, string contextSignature = "");
        void RequestFrontendMenuInputMode(string reason, string semanticSource, string contextSignature = "");
        void RequestPauseOverlayInputMode(string reason, string semanticSource, string contextSignature = "");
        void Clear(string reason = null);
    }
}
