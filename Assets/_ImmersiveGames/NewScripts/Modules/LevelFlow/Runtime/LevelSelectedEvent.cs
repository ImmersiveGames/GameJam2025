using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Evento canônico de seleção de level para correlação entre navegação, conteúdo e gameplay.
    /// </summary>
    public readonly struct LevelSelectedEvent : IEvent
    {
        public LevelSelectedEvent(
            LevelId levelId,
            SceneRouteId routeId,
            TransitionStyleId styleId,
            string contentId,
            string reason,
            int selectionVersion,
            LevelContextSignature levelSignature)
        {
            LevelId = levelId;
            RouteId = routeId;
            StyleId = styleId;
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = levelSignature;
        }

        public LevelId LevelId { get; }
        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public LevelContextSignature LevelSignature { get; }
    }
}
