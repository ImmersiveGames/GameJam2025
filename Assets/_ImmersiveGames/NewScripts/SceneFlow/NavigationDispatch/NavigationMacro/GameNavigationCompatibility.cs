using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro
{
    /// <summary>
    /// Compatibility aliases and historical intent mappings for Navigation.
    /// </summary>
    public static class GameNavigationCompatibility
    {
        public static NavigationIntentId Defeat => NavigationIntentId.FromName("defeat");
        public static NavigationIntentId Restart => NavigationIntentId.FromName("restart");
        public static NavigationIntentId ExitToMenu => NavigationIntentId.FromName("exit-to-menu");

        public static IReadOnlyList<NavigationIntentId> AllAliases => _allAliases;

        private static readonly NavigationIntentId[] _allAliases =
        {
            Defeat,
            Restart,
            ExitToMenu,
        };

        public static bool TryMapToCompatibilityKind(string intentId, out GameNavigationIntentKind kind)
        {
            string normalized = NavigationIntentId.Normalize(intentId);
            if (string.Equals(normalized, Defeat.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Gameplay;
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

        public static NavigationIntentId GetCompatibilityId(GameNavigationIntentKind kind)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Restart:
                    return Restart;
                case GameNavigationIntentKind.ExitToMenu:
                    return ExitToMenu;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}

