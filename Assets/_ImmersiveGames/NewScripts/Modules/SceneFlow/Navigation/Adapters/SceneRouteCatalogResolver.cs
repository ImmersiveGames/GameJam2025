using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters
{
    /// <summary>
    /// Adaptador default: expõe ISceneRouteCatalog como ISceneRouteResolver.
    /// </summary>
    public sealed class SceneRouteCatalogResolver : ISceneRouteResolver
    {
        private readonly ISceneRouteCatalog _catalog;

        public SceneRouteCatalogResolver(ISceneRouteCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool TryResolve(SceneRouteId routeId, out SceneRouteDefinition routeDefinition)
            => _catalog.TryGet(routeId, out routeDefinition);
    }
}
