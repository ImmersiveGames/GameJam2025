using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Nomes canônicos de profiles usados pelo SceneFlow (Fade/WorldLifecycle/Logs).
    /// Centraliza strings para evitar drift.
    ///
    /// Regra:
    /// - "startup" é exclusivo do boot (NewBootstrap -> Menu).
    /// - "frontend" é o profile canônico para navegação de Menu em runtime.
    /// - "gameplay" é o profile canônico para Gameplay.
    /// </summary>
    public static class SceneFlowProfileNames
    {
        public const string Startup = "startup";
        public const string Frontend = "frontend";
        public const string Gameplay = "gameplay";

        public static string Normalize(string profileName)
        {
            return string.IsNullOrWhiteSpace(profileName) ? string.Empty : profileName.Trim();
        }

        public static bool IsStartup(string profileName)
        {
            return string.Equals(Normalize(profileName), Startup, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFrontend(string profileName)
        {
            return string.Equals(Normalize(profileName), Frontend, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsGameplay(string profileName)
        {
            return string.Equals(Normalize(profileName), Gameplay, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStartupOrFrontend(string profileName)
        {
            return IsStartup(profileName) || IsFrontend(profileName);
        }
    }
}
