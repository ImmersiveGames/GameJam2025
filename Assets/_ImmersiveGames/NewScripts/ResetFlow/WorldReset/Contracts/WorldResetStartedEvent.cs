using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Contracts
{
    /// <summary>
    /// Evento canônico publicado quando um WorldReset inicia.
    /// Um único contrato atende macro reset, level reset e observabilidade.
    /// </summary>
    public readonly struct WorldResetStartedEvent : IEvent
    {
        public WorldResetStartedEvent(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string contextSignature,
            PhaseContextSignature phaseSignature,
            WorldResetOrigin origin,
            string targetScene,
            string sourceSignature = null)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            ContextSignature = Normalize(contextSignature);
            PhaseSignature = phaseSignature;
            TargetScene = Normalize(targetScene);
            Origin = origin;
            SourceSignature = Normalize(sourceSignature);
        }

        public WorldResetStartedEvent(string contextSignature, string reason)
            : this(
                ResetKind.Macro,
                SceneRouteId.None,
                reason,
                contextSignature,
                PhaseContextSignature.Empty,
                WorldResetOrigin.Unknown,
                string.Empty,
                contextSignature)
        {
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public string ContextSignature { get; }
        public string SourceSignature { get; }
        public PhaseContextSignature PhaseSignature { get; }
        public string TargetScene { get; }
        public WorldResetOrigin Origin { get; }

        public string MacroSignature => ContextSignature;
        public bool HasContextSignature => !string.IsNullOrWhiteSpace(ContextSignature);
        public bool HasPhaseSignature => PhaseSignature.IsValid;

        public override string ToString()
        {
            return $"WorldResetStartedEvent(Kind='{Kind}', Route='{MacroRouteId}', ContextSignature='{ContextSignature}', PhaseSignature='{PhaseSignature}', TargetScene='{TargetScene}', Reason='{Reason}', Origin='{Origin}')";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

