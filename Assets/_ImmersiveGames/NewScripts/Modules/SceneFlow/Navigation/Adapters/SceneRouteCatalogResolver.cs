using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters
{
    /// <summary>
    /// Adaptador default: expõe ISceneRouteCatalog como ISceneRouteResolver.
    /// </summary>
        /// <summary>
    /// OWNER: resolucao routeId -> SceneRouteDefinition em runtime.
    /// NAO E OWNER: validacao de policy de reset e conteudo dos assets de rota.
    /// PUBLISH/CONSUME: sem EventBus; consumido por SceneTransitionService.
    /// Fases tocadas: RouteExecutionPlan (resolucao previa da rota).
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


