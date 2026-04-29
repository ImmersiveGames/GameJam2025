using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts
{
    public interface ISessionIntegrationInputModeEmitter
    {
        void RequestGameplayInputMode(string reason, string semanticSource, string contextSignature = "");
        void RequestFrontendMenuInputMode(string reason, string semanticSource, string contextSignature = "");
        void RequestPauseOverlayInputMode(string reason, string semanticSource, string contextSignature = "");
    }

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
        void Clear(string reason = null);
    }

    /// <summary>
    /// Porta minima de leitura para consumo operacional (spawn/reset) sem expor seam amplo.
    /// </summary>
    public interface ISpawnResetParticipationReadPort
    {
        bool TryGetCurrent(out SpawnResetParticipationSnapshot snapshot);
    }

    public readonly struct SpawnResetParticipationSnapshot
    {
        public SpawnResetParticipationSnapshot(
            string signature,
            string readinessState,
            string primaryParticipantId,
            string localParticipantId,
            string localBindingHint)
        {
            Signature = Normalize(signature);
            ReadinessState = Normalize(readinessState);
            PrimaryParticipantId = Normalize(primaryParticipantId);
            LocalParticipantId = Normalize(localParticipantId);
            LocalBindingHint = Normalize(localBindingHint);
        }

        public string Signature { get; }
        public string ReadinessState { get; }
        public string PrimaryParticipantId { get; }
        public string LocalParticipantId { get; }
        public string LocalBindingHint { get; }

        public bool HasSignature => !string.IsNullOrWhiteSpace(Signature);

        public static SpawnResetParticipationSnapshot Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

