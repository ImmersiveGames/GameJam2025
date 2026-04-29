using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts
{
    /// <summary>
    /// Evento canônico publicado quando um WorldReset conclui.
    /// Consumido por gate, loading, gameplay e observabilidade.
    /// </summary>
    public readonly struct WorldResetCompletedEvent : IEvent
    {
        public WorldResetCompletedEvent(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string contextSignature,
            PhaseContextSignature phaseSignature,
            WorldResetOutcome outcome,
            string detail,
            WorldResetOrigin origin,
            string targetScene,
            string sourceSignature = null)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            ContextSignature = Normalize(contextSignature);
            PhaseSignature = phaseSignature;
            Outcome = outcome;
            Detail = Normalize(detail);
            TargetScene = Normalize(targetScene);
            Origin = origin;
            SourceSignature = Normalize(sourceSignature);
        }

        public WorldResetCompletedEvent(string contextSignature, string reason)
            : this(
                ResetKind.Macro,
                SceneRouteId.None,
                reason,
                contextSignature,
                PhaseContextSignature.Empty,
                WorldResetOutcome.Completed,
                string.Empty,
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
        public WorldResetOutcome Outcome { get; }
        public string Detail { get; }
        public string TargetScene { get; }
        public WorldResetOrigin Origin { get; }

        public string MacroSignature => ContextSignature;
        public bool HasContextSignature => !string.IsNullOrWhiteSpace(ContextSignature);
        public bool HasPhaseSignature => PhaseSignature.IsValid;

        public override string ToString()
        {
            return $"WorldResetCompletedEvent(Kind='{Kind}', Route='{MacroRouteId}', ContextSignature='{ContextSignature}', PhaseSignature='{PhaseSignature}', TargetScene='{TargetScene}', Reason='{Reason}', Outcome='{Outcome}', Detail='{Detail}', Origin='{Origin}')";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

