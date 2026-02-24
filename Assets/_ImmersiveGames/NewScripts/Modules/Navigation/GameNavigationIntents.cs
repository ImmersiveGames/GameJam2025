namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Compatibilidade de ids string para intents de navegação.
    /// Migração: preferir <see cref="GameNavigationIntentKind"/> + core slots.
    /// </summary>
    public static class GameNavigationIntents
    {
        [System.Obsolete("Use GameNavigationIntentKind.Menu e resolução por core slots. Constante string mantida apenas para compat/tooling.")]
        public const string ToMenu = "to-menu";

        [System.Obsolete("Use GameNavigationIntentKind.Gameplay e resolução por core slots. Constante string mantida apenas para compat/tooling.")]
        public const string ToGameplay = "to-gameplay";

        public static string FromKind(GameNavigationIntentKind kind)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Menu:
                    return "to-menu";
                case GameNavigationIntentKind.Gameplay:
                    return "to-gameplay";
                case GameNavigationIntentKind.GameOver:
                    return "to-gameover";
                case GameNavigationIntentKind.Victory:
                    return "to-victory";
                case GameNavigationIntentKind.Restart:
                    return "to-restart";
                case GameNavigationIntentKind.ExitToMenu:
                    return "exit-to-menu";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
