using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Evento V2 de telemetria/observabilidade de reset.
    /// V1 continua sendo o plano de gate/correlacao do SceneFlow.
    /// </summary>
    public readonly struct WorldLifecycleResetRequestedV2Event : IEvent
    {
        public WorldLifecycleResetRequestedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            MacroSignature = Normalize(macroSignature);
            LevelSignature = levelSignature;
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Evento V2 de conclusao de reset para telemetria/observabilidade.
    /// V1 continua sendo o plano de gate/correlacao do SceneFlow.
    /// </summary>
    public readonly struct WorldLifecycleResetCompletedV2Event : IEvent
    {
        public WorldLifecycleResetCompletedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            MacroSignature = Normalize(macroSignature);
            LevelSignature = levelSignature;
            Success = success;
            Notes = Normalize(notes);
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }
        public bool Success { get; }
        public string Notes { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
