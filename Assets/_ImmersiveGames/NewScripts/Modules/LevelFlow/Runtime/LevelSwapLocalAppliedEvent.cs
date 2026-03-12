using System.ComponentModel;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelSwapLocalAppliedEvent : IEvent
    {
        public LevelSwapLocalAppliedEvent(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
        }

        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        // Compat temporaria com trilhos legados; nao faz parte do contrato canonico.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. Canon usa LevelRef.")]
        public LevelId LevelId => LevelRef != null ? LevelId.FromName(LevelRef.name) : LevelId.None;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. Canon usa MacroRouteId.")]
        public SceneRouteId RouteId => MacroRouteId;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. Canon nao usa contentId.")]
        public string ContentId => string.Empty;
    }
}
