namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Contrato para catálogos de rotas do SceneFlow.
    /// </summary>
    public interface ISceneRouteCatalog
    {
        bool TryGet(SceneRouteId routeId, out SceneRouteDefinition route);
    }
}
