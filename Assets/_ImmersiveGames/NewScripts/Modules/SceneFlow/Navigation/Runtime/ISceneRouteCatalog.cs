using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Contrato para catálogos de rotas do SceneFlow.
    /// </summary>
    public interface ISceneRouteCatalog
    {
        IEnumerable<SceneRouteId> RouteIds { get; }

        bool TryGet(SceneRouteId routeId, out SceneRouteDefinition route);
    }
}
