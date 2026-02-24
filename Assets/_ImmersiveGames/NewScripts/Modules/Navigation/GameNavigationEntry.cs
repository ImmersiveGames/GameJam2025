using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Resultado de resolução de um intent de navegação (route + style + payload).
    /// </summary>
    public readonly struct GameNavigationEntry
    {
        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public SceneTransitionPayload Payload { get; }

        public GameNavigationEntry(
            SceneRouteId routeId,
            TransitionStyleId styleId,
            SceneTransitionPayload payload)
        {
            RouteId = routeId;
            StyleId = styleId;
            Payload = payload ?? SceneTransitionPayload.Empty;
        }

        public bool IsValid => RouteId.IsValid;

        public override string ToString()
            => $"routeId='{RouteId}', styleId='{StyleId}', payload=({Payload})";
    }
}
