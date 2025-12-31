using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.Navigation
{
    /// <summary>
    /// Catálogo de rotas/plans. Um único lugar para editar nomes de cenas e combinações.
    /// </summary>
    public sealed class GameNavigationCatalog
    {
        /// <summary>
        /// IDs canônicos (evita strings soltas espalhadas no código).
        /// </summary>
        public static class Routes
        {
            public const string ToMenu = "to_menu";
            public const string ToGameplay = "to_gameplay";
        }

        /// <summary>
        /// Nomes de cenas (Unity: SceneManager.GetSceneByName).
        /// Centralize aqui para reduzir erro humano.
        /// </summary>
        public static class Scenes
        {
            public const string NewBootstrap = "NewBootstrap";
            public const string Menu = "MenuScene";
            public const string UIGlobal = "UIGlobalScene";

            // PLACEHOLDER: ajuste para o nome real quando você enviar o arquivo que o referencia.
            public const string Gameplay = "GameplayScene";
        }

        private readonly Dictionary<string, SceneTransitionRequest> _routes;

        public GameNavigationCatalog(Dictionary<string, SceneTransitionRequest> routes)
        {
            _routes = routes ?? new Dictionary<string, SceneTransitionRequest>(StringComparer.Ordinal);
        }

        public bool TryGet(string routeId, out SceneTransitionRequest request)
            => _routes.TryGetValue(routeId ?? string.Empty, out request);

        public IReadOnlyCollection<string> RouteIds => _routes.Keys;

        /// <summary>
        /// Constrói o catálogo mínimo do jogo atual:
        /// - Menu -> Gameplay
        /// - Gameplay -> Menu
        /// Observação: UIGlobal é mantida carregada (não descarregamos).
        /// </summary>
        public static GameNavigationCatalog CreateDefaultMinimal()
        {
            var routes = new Dictionary<string, SceneTransitionRequest>(StringComparer.Ordinal)
            {
                // Menu -> Gameplay
                [Routes.ToGameplay] = new SceneTransitionRequest(
                    scenesToLoad: new[] { Scenes.Gameplay },
                    scenesToUnload: new[] { Scenes.Menu },
                    targetActiveScene: Scenes.Gameplay,
                    useFade: true,
                    transitionProfileName: SceneFlowProfileNames.Gameplay),

                // Gameplay -> Menu
                [Routes.ToMenu] = new SceneTransitionRequest(
                    scenesToLoad: new[] { Scenes.Menu },
                    scenesToUnload: new[] { Scenes.Gameplay },
                    targetActiveScene: Scenes.Menu,
                    useFade: true,
                    transitionProfileName: SceneFlowProfileNames.Frontend)
            };

            return new GameNavigationCatalog(routes);
        }
    }
}
