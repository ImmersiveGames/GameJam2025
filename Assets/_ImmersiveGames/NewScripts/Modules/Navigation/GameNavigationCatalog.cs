using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo hardcoded de navegação.
    ///
    /// Observação:
    /// - Uso recomendado apenas para debug/local dev.
    /// - Em produção, utilize <see cref="GameNavigationCatalogAsset"/>.
    /// </summary>
    [Obsolete("Debug only. Use GameNavigationCatalogAsset with SceneRoute/TransitionStyle catalogs.")]
    public sealed class GameNavigationCatalog : IGameNavigationCatalog
    {
        // Scene names (Unity: SceneManager.GetActiveScene().name)
        public const string SceneMenu = "MenuScene";
        public const string SceneGameplay = "GameplayScene";
        public const string SceneUIGlobal = "UIGlobalScene";
        public const string SceneNewBootstrap = "NewBootstrap";

        private static readonly IReadOnlyCollection<string> _routeIds = new[]
        {
            GameNavigationIntents.ToMenu,
            GameNavigationIntents.ToGameplay
        };

        public IReadOnlyCollection<string> RouteIds => _routeIds;

        /// <summary>
        /// Factory de debug: fornece entradas mínimas hardcoded.
        /// </summary>
        public static GameNavigationCatalog CreateDebugMinimal() => new();

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
            if (string.Equals(routeId, GameNavigationIntents.ToGameplay, StringComparison.Ordinal))
            {
                entry = BuildMenuToGameplay();
                return true;
            }

            if (string.Equals(routeId, GameNavigationIntents.ToMenu, StringComparison.Ordinal))
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
            var payload = SceneTransitionPayload.CreateSceneData(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneMenu },
                targetActiveScene: SceneGameplay);

            return new GameNavigationEntry(
                routeId: SceneRouteId.FromName(GameNavigationIntents.ToGameplay),
                styleId: TransitionStyleId.FromName("style.gameplay"),
                payload: payload);
        }

        /// <summary>
        /// Gameplay -> Menu.
        /// </summary>
        public GameNavigationEntry BuildGameplayToMenu()
        {
            var payload = SceneTransitionPayload.CreateSceneData(
                scenesToLoad: new[] { SceneMenu, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneMenu);

            return new GameNavigationEntry(
                routeId: SceneRouteId.FromName(GameNavigationIntents.ToMenu),
                styleId: TransitionStyleId.FromName("style.frontend"),
                payload: payload);
        }

        /// <summary>
        /// Gameplay -> Gameplay (reload).
        /// </summary>
        public GameNavigationEntry BuildGameplayReload()
        {
            var payload = SceneTransitionPayload.CreateSceneData(
                scenesToLoad: new[] { SceneGameplay, SceneUIGlobal },
                scenesToUnload: new[] { SceneGameplay },
                targetActiveScene: SceneGameplay);

            return new GameNavigationEntry(
                routeId: SceneRouteId.FromName(GameNavigationIntents.ToGameplay),
                styleId: TransitionStyleId.FromName("style.gameplay"),
                payload: payload);
        }
    }
}
