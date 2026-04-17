using System;
using System.Collections.Generic;

namespace ImmersiveGames.GameJam2025.Orchestration.Navigation
{
    /// <summary>
    /// Canonical code source for navigation core intents.
    /// </summary>
    public static class GameNavigationIntents
    {
        public static NavigationIntentId Menu => NavigationIntentId.FromName("to-menu");
        public static NavigationIntentId Gameplay => NavigationIntentId.FromName("to-gameplay");
        public static NavigationIntentId GameOver => NavigationIntentId.FromName("gameover");
        public static NavigationIntentId Victory => NavigationIntentId.FromName("victory");

        public static IReadOnlyList<NavigationIntentId> RequiredCore => _requiredCore;
        public static IReadOnlyList<NavigationIntentId> OptionalCore => _optionalCore;
        public static IReadOnlyList<NavigationIntentId> AllCanonical => _allCanonical;

        private static readonly NavigationIntentId[] _requiredCore =
        {
            Menu,
            Gameplay,
        };

        private static readonly NavigationIntentId[] _optionalCore =
        {
            GameOver,
            Victory,
        };

        private static readonly NavigationIntentId[] _allCanonical =
        {
            Menu,
            Gameplay,
            GameOver,
            Victory,
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static bool TryMapToCoreKind(NavigationIntentId intentId, out GameNavigationIntentKind kind)
            => TryMapToCoreKind(intentId.Value, out kind);

        public static bool TryMapToCoreKind(string intentId, out GameNavigationIntentKind kind)
        {
            string normalized = NavigationIntentId.Normalize(intentId);
            if (string.Equals(normalized, Menu.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Menu;
                return true;
            }

            if (string.Equals(normalized, Gameplay.Value, StringComparison.OrdinalIgnoreCase))
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

            kind = default;
            return false;
        }
    }
}

