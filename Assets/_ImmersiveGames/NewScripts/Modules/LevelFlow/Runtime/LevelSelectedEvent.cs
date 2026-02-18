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
            string contentId,
            string reason,
            int selectionVersion,
            string contextSignature)
        {
            LevelId = levelId;
            RouteId = routeId;
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
        }

        public LevelId LevelId { get; }
        public SceneRouteId RouteId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string ContextSignature { get; }
    }
}
