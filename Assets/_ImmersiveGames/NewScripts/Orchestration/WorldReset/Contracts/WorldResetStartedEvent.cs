using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Contracts
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
            LevelContextSignature levelSignature,
            WorldResetOrigin origin,
            string targetScene,
            string sourceSignature = null)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            ContextSignature = Normalize(contextSignature);
            LevelSignature = levelSignature;
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
                LevelContextSignature.Empty,
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
        public LevelContextSignature LevelSignature { get; }
        public string TargetScene { get; }
        public WorldResetOrigin Origin { get; }

        public string MacroSignature => ContextSignature;
        public bool HasContextSignature => !string.IsNullOrWhiteSpace(ContextSignature);
        public bool HasLevelSignature => LevelSignature.IsValid;

        public override string ToString()
        {
            return $"WorldResetStartedEvent(Kind='{Kind}', Route='{MacroRouteId}', ContextSignature='{ContextSignature}', LevelSignature='{LevelSignature}', TargetScene='{TargetScene}', Reason='{Reason}', Origin='{Origin}')";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
