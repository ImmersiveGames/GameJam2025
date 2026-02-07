using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo de rotas/plans (hardcoded).
    ///
    /// Nota:
    /// - Continua existindo como fallback mínimo e como referência canônica de ids (Routes.*).
    /// - Para rotas configuráveis, use <see cref="GameNavigationCatalogAsset"/>.
    /// </summary>
    public sealed class GameNavigationCatalog : IGameNavigationCatalog
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
        /// Resolve uma rota canônica em um <see cref="GameNavigationEntry"/>.
        /// </summary>
        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            entry = default;

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            // StringComparer.Ordinal por estabilidade (ids canônicos em lower-hyphen).
            if (string.Equals(routeId, Routes.ToGameplay, StringComparison.Ordinal))
            {
                entry = BuildMenuToGameplay();
                return true;
            }

            if (string.Equals(routeId, Routes.ToMenu, StringComparison.Ordinal))
            {
                entry = BuildGameplayToMenu();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Menu -> Gameplay.
        /// </summary>
        public GameNavigationEntry BuildMenuToGameplay()
        {
            var payload = SceneTransitionPayload.FromLegacy(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneMenu },
                targetActiveScene: SceneGameplay,
                useFade: true,
                legacyProfileId: SceneFlowProfileId.Gameplay);

            return new GameNavigationEntry(
                routeId: LegacyRouteStringToRouteIdAdapter.Adapt(Routes.ToGameplay),
                styleId: LegacyProfileIdToStyleIdAdapter.Adapt(SceneFlowProfileId.Gameplay),
                payload: payload);
        }

        /// <summary>
        /// Gameplay -> Menu.
        /// </summary>
        public GameNavigationEntry BuildGameplayToMenu()
        {
            var payload = SceneTransitionPayload.FromLegacy(
                scenesToLoad: new[] { SceneMenu, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneMenu,
                useFade: true,
                legacyProfileId: SceneFlowProfileId.Frontend);

            return new GameNavigationEntry(
                routeId: LegacyRouteStringToRouteIdAdapter.Adapt(Routes.ToMenu),
                styleId: LegacyProfileIdToStyleIdAdapter.Adapt(SceneFlowProfileId.Frontend),
                payload: payload);
        }

        /// <summary>
        /// Gameplay -> Gameplay (reload).
        /// </summary>
        public GameNavigationEntry BuildGameplayReload()
        {
            var payload = SceneTransitionPayload.FromLegacy(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneGameplay,
                useFade: true,
                legacyProfileId: SceneFlowProfileId.Gameplay);

            return new GameNavigationEntry(
                routeId: LegacyRouteStringToRouteIdAdapter.Adapt(Routes.ToGameplay),
                styleId: LegacyProfileIdToStyleIdAdapter.Adapt(SceneFlowProfileId.Gameplay),
                payload: payload);
        }
    }
}
