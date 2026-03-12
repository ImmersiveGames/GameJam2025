using System.ComponentModel;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelSelectedEvent : IEvent
    {
        public LevelSelectedEvent(
            SceneRouteId macroRouteId,
            LevelDefinitionAsset levelRef,
            int selectionVersion,
            string reason,
            string levelSignature)
        {
            MacroRouteId = macroRouteId;
            LevelRef = levelRef;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            StyleId = TransitionStyleId.None;
        }

        // Compat temporaria com trilhos legados; nao faz parte do contrato canonico.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [System.Obsolete("Compat temporaria apenas. Canonical flow uses LevelDefinitionAsset reference.")]
        public LevelSelectedEvent(
            LevelId levelId,
            SceneRouteId routeId,
            TransitionStyleId styleId,
            string contentId,
            string reason,
            int selectionVersion,
            LevelContextSignature levelSignature)
        {
            MacroRouteId = routeId;
            LevelRef = null;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            LevelSignature = levelSignature.IsValid ? levelSignature.Value : string.Empty;
            StyleId = styleId;
        }

        public SceneRouteId MacroRouteId { get; }
        public LevelDefinitionAsset LevelRef { get; }
        public int SelectionVersion { get; }
        public string Reason { get; }
        public string LevelSignature { get; }
        public TransitionStyleId StyleId { get; }

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
