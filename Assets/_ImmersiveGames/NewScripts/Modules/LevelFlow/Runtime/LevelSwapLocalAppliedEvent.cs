using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Evento de domínio LevelFlow; NÃO é MacroSignature.
    /// </summary>
    public readonly struct LevelSwapLocalAppliedEvent : IEvent
    {
        public LevelSwapLocalAppliedEvent(
            LevelId levelId,
            SceneRouteId routeId,
            SceneRouteId macroRouteId,
            string contentId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            LevelId = levelId;
            RouteId = routeId;
            MacroRouteId = macroRouteId;
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
        }

        public LevelId LevelId { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteId MacroRouteId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }
    }
}
