using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro
{
    /// <summary>
    /// Resultado de resolucao de um intent de navegacao (route + style + payload).
    /// Direct-ref-first: StyleRef e a fonte principal; labels sao apenas observabilidade.
    /// </summary>
    public readonly struct GameNavigationEntry
    {
        public SceneRouteId RouteId { get; }
        public SceneRouteDefinitionAsset RouteRef { get; }
        public TransitionStyleAsset StyleRef { get; }
        public SceneTransitionPayload Payload { get; }
        public string StyleLabel => StyleRef != null ? StyleRef.StyleLabel : string.Empty;

        public GameNavigationEntry(
            SceneRouteId routeId,
            TransitionStyleAsset styleRef,
            SceneTransitionPayload payload,
            SceneRouteDefinitionAsset routeRef = null)
        {
            RouteId = routeId;
            RouteRef = routeRef;
            StyleRef = styleRef;
            Payload = payload ?? SceneTransitionPayload.Empty;
        }

        public bool IsValid => RouteId.IsValid;

        public override string ToString()
            => $"routeId='{RouteId}', style='{StyleLabel}', styleRef='{(StyleRef != null ? StyleRef.name : "<null>")}', payload=({Payload})";
    }
}

