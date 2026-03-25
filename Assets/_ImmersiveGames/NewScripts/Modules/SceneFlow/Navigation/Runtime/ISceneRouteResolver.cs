namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// ResolvePlayerActor SceneRouteId para SceneRouteDefinition.
    /// </summary>
    public interface ISceneRouteResolver
    {
        bool TryResolve(SceneRouteId routeId, out SceneRouteDefinition routeDefinition);
    }
}
