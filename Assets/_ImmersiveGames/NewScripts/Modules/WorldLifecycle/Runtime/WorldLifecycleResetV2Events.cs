using System.ComponentModel;
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
        private readonly LevelId _legacyLevelId;
        private readonly string _legacyContentId;

        public WorldLifecycleResetRequestedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature)
            : this(kind, macroRouteId, reason, macroSignature, levelSignature, LevelId.None, string.Empty)
        {
        }

        private WorldLifecycleResetRequestedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            LevelId legacyLevelId,
            string legacyContentId)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            MacroSignature = Normalize(macroSignature);
            LevelSignature = levelSignature;
            _legacyLevelId = legacyLevelId;
            _legacyContentId = Normalize(legacyContentId);
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }

        // Compat temporaria com telemetria legacy; nao faz parte do shape canonico de V2.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. V2 canonico usa LevelSignature.")]
        public LevelId LevelId => _legacyLevelId;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. V2 canonico usa LevelSignature.")]
        public string ContentId => _legacyContentId;

        internal static WorldLifecycleResetRequestedV2Event CreateWithLegacyCompat(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            LevelId legacyLevelId,
            string legacyContentId)
        {
            return new WorldLifecycleResetRequestedV2Event(
                kind,
                macroRouteId,
                reason,
                macroSignature,
                levelSignature,
                legacyLevelId,
                legacyContentId);
        }

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
        private readonly LevelId _legacyLevelId;
        private readonly string _legacyContentId;

        public WorldLifecycleResetCompletedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes)
            : this(kind, macroRouteId, reason, macroSignature, levelSignature, success, notes, LevelId.None, string.Empty)
        {
        }

        private WorldLifecycleResetCompletedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes,
            LevelId legacyLevelId,
            string legacyContentId)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            Reason = Normalize(reason);
            MacroSignature = Normalize(macroSignature);
            LevelSignature = levelSignature;
            Success = success;
            Notes = Normalize(notes);
            _legacyLevelId = legacyLevelId;
            _legacyContentId = Normalize(legacyContentId);
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }
        public bool Success { get; }
        public string Notes { get; }

        // Compat temporaria com telemetria legacy; nao faz parte do shape canonico de V2.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. V2 canonico usa LevelSignature.")]
        public LevelId LevelId => _legacyLevelId;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. V2 canonico usa LevelSignature.")]
        public string ContentId => _legacyContentId;

        internal static WorldLifecycleResetCompletedV2Event CreateWithLegacyCompat(
            ResetKind kind,
            SceneRouteId macroRouteId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes,
            LevelId legacyLevelId,
            string legacyContentId)
        {
            return new WorldLifecycleResetCompletedV2Event(
                kind,
                macroRouteId,
                reason,
                macroSignature,
                levelSignature,
                success,
                notes,
                legacyLevelId,
                legacyContentId);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
