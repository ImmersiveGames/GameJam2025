using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo de rotas/plans. Um único lugar para editar nomes de cenas e combinações.
    ///
    /// Contrato esperado pelo <see cref="GameNavigationService" />:
    /// - expõe RouteIds (para logging)
    /// - expõe Routes (ids canônicos)
    /// - expõe TryGet(routeId, out request)
    /// - expõe CreateDefaultMinimal()
    /// </summary>
    public sealed class GameNavigationCatalog
    {
        // Scene names (Unity: SceneManager.GetActiveScene().name)
        public const string SceneMenu = "MenuScene";
        public const string SceneGameplay = "GameplayScene";
        public const string SceneUIGlobal = "UIGlobalScene";
        public const string SceneNewBootstrap = "NewBootstrap";

        public static class Routes
        {
            public const string ToMenu = "to-menu";
            public const string ToGameplay = "to-gameplay";
        }

        private static readonly IReadOnlyCollection<string> _routeIds = new[]
        {
            Routes.ToMenu,
            Routes.ToGameplay
        };

        public IReadOnlyCollection<string> RouteIds => _routeIds;

        /// <summary>
        /// Factory padrão usada pelo GameNavigationService quando nenhum catálogo é injetado.
        /// </summary>
        public static GameNavigationCatalog CreateDefaultMinimal() => new();

        /// <summary>
        /// Resolve uma rota canônica em um <see cref="SceneTransitionRequest"/>.
        /// </summary>
        public bool TryGet(string routeId, out SceneTransitionRequest request)
        {
            request = null;

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            // StringComparer.Ordinal por estabilidade (ids canônicos em lower-hyphen).
            if (string.Equals(routeId, Routes.ToGameplay, StringComparison.Ordinal))
            {
                request = BuildMenuToGameplay();
                return true;
            }

            if (string.Equals(routeId, Routes.ToMenu, StringComparison.Ordinal))
            {
                request = BuildGameplayToMenu();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Menu -> Gameplay.
        /// </summary>
        public SceneTransitionRequest BuildMenuToGameplay()
        {
            return new SceneTransitionRequest(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneMenu },
                targetActiveScene: SceneGameplay,
                useFade: true,
                transitionProfileId: SceneFlowProfileId.Gameplay);
        }

        /// <summary>
        /// Gameplay -> Menu.
        /// </summary>
        public SceneTransitionRequest BuildGameplayToMenu()
        {
            return new SceneTransitionRequest(
                scenesToLoad: new[] { SceneMenu, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneMenu,
                useFade: true,
                transitionProfileId: SceneFlowProfileId.Frontend);
        }

        /// <summary>
        /// Gameplay -> Gameplay (reload).
        /// </summary>
        public SceneTransitionRequest BuildGameplayReload()
        {
            return new SceneTransitionRequest(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneGameplay,
                useFade: true,
                transitionProfileId: SceneFlowProfileId.Gameplay);
        }
    }
}
