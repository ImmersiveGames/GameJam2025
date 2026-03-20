using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Fonte canonica em codigo para os intents core e aliases oficiais de navigation.
    /// </summary>
    public static class GameNavigationIntents
    {
        public static NavigationIntentId Menu => NavigationIntentId.FromName("to-menu");
        public static NavigationIntentId Gameplay => NavigationIntentId.FromName("to-gameplay");
        public static NavigationIntentId GameOver => NavigationIntentId.FromName("gameover");
        public static NavigationIntentId Defeat => NavigationIntentId.FromName("defeat");
        public static NavigationIntentId Victory => NavigationIntentId.FromName("victory");
        public static NavigationIntentId Restart => NavigationIntentId.FromName("restart");
        public static NavigationIntentId ExitToMenu => NavigationIntentId.FromName("exit-to-menu");

        public static IReadOnlyList<NavigationIntentId> RequiredCore => _requiredCore;
        public static IReadOnlyList<NavigationIntentId> OptionalCoreAndAliases => _optionalCoreAndAliases;
        public static IReadOnlyList<NavigationIntentId> AllCanonicalAndAliases => _allCanonicalAndAliases;

        private static readonly NavigationIntentId[] _requiredCore =
        {
            Menu,
            Gameplay,
        };

        private static readonly NavigationIntentId[] _optionalCoreAndAliases =
        {
            GameOver,
            Defeat,
            Victory,
            Restart,
            ExitToMenu,
        };

        private static readonly NavigationIntentId[] _allCanonicalAndAliases =
        {
            Menu,
            Gameplay,
            GameOver,
            Defeat,
            Victory,
            Restart,
            ExitToMenu,
        };

        public static NavigationIntentId GetCoreId(GameNavigationIntentKind kind)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Menu:
                    return Menu;
                case GameNavigationIntentKind.Gameplay:
                    return Gameplay;
                case GameNavigationIntentKind.GameOver:
                    return GameOver;
                case GameNavigationIntentKind.Victory:
                    return Victory;
                case GameNavigationIntentKind.Restart:
                    return Restart;
                case GameNavigationIntentKind.ExitToMenu:
                    return ExitToMenu;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static bool TryMapToCoreKind(NavigationIntentId intentId, out GameNavigationIntentKind kind)
        {
            string normalized = NavigationIntentId.Normalize(intentId.Value);
            if (string.Equals(normalized, Menu.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Menu;
                return true;
            }

            if (string.Equals(normalized, Gameplay.Value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, Defeat.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Gameplay;
                return true;
            }

            if (string.Equals(normalized, GameOver.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.GameOver;
                return true;
            }

            if (string.Equals(normalized, Victory.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Victory;
                return true;
            }

            if (string.Equals(normalized, Restart.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Restart;
                return true;
            }

            if (string.Equals(normalized, ExitToMenu.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.ExitToMenu;
                return true;
            }

            kind = default;
            return false;
        }
    }
}
