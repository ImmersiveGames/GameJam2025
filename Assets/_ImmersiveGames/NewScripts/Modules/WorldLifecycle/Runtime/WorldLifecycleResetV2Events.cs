using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Evento canônico V2 para observabilidade explícita de reset (macro x level).
    /// Mantém compatibilidade com o evento legado usado pelo gate do SceneFlow.
    /// </summary>
    public readonly struct WorldLifecycleResetRequestedV2Event : IEvent
    {
        public WorldLifecycleResetRequestedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            LevelId levelId,
            string contentId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            LevelId = levelId;
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            MacroSignature = string.IsNullOrWhiteSpace(macroSignature) ? string.Empty : macroSignature.Trim();
            LevelSignature = levelSignature;
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public LevelId LevelId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }
    }

    /// <summary>
    /// Evento canônico V2 de conclusão de reset (macro x level).
    /// </summary>
    public readonly struct WorldLifecycleResetCompletedV2Event : IEvent
    {
        public WorldLifecycleResetCompletedV2Event(
            ResetKind kind,
            SceneRouteId macroRouteId,
            LevelId levelId,
            string contentId,
            string reason,
            string macroSignature,
            LevelContextSignature levelSignature,
            bool success,
            string notes)
        {
            Kind = kind;
            MacroRouteId = macroRouteId;
            LevelId = levelId;
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            MacroSignature = string.IsNullOrWhiteSpace(macroSignature) ? string.Empty : macroSignature.Trim();
            LevelSignature = levelSignature;
            Success = success;
            Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
        }

        public ResetKind Kind { get; }
        public SceneRouteId MacroRouteId { get; }
        public LevelId LevelId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public string MacroSignature { get; }
        public LevelContextSignature LevelSignature { get; }
        public bool Success { get; }
        public string Notes { get; }
    }
}
