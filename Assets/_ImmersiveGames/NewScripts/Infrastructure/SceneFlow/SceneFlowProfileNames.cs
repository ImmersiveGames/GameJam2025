using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    public enum SceneFlowProfileId
    {
        Unknown = 0,
        Startup = 1,
        Frontend = 2,
        Gameplay = 3,
    }

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
        public static bool TryParse(string profileName, out SceneFlowProfileId id)
        {
            var n = Normalize(profileName);

            if (n == Startup)
            {
                id = SceneFlowProfileId.Startup;
                return true;
            }

            if (n == Frontend)
            {
                id = SceneFlowProfileId.Frontend;
                return true;
            }

            if (n == Gameplay)
            {
                id = SceneFlowProfileId.Gameplay;
                return true;
            }

            id = SceneFlowProfileId.Unknown;
            return false;
        }

        public static SceneFlowProfileId ParseOrUnknown(string profileName)
        {
            return TryParse(profileName, out var id) ? id : SceneFlowProfileId.Unknown;
        }

        public static string ToName(SceneFlowProfileId id)
        {
            switch (id)
            {
                case SceneFlowProfileId.Startup:
                    return Startup;

                case SceneFlowProfileId.Frontend:
                    return Frontend;

                case SceneFlowProfileId.Gameplay:
                    return Gameplay;

                default:
                    return string.Empty;
            }
        }

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
