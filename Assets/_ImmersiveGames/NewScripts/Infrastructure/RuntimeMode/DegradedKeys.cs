namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Chaves padronizadas para logs de degradação (DEGRADED_MODE).
    /// Objetivo: facilitar busca no log e evitar variações de escrita entre módulos.
    /// </summary>
    public static class DegradedKeys
    {
        public static class Feature
        {
            public const string SceneFlow = "SceneFlow";
            public const string Loading = "Loading";
            public const string Fade = "Fade";
            public const string WorldLifecycle = "WorldLifecycle";
            public const string GameLoop = "GameLoop";
            public const string Gameplay = "Gameplay";
            public const string Levels = "Levels";
            public const string ContentSwap = "ContentSwap";
            public const string Navigation = "Navigation";
            public const string Gates = "Gates";
            public const string ControlModes = "ControlModes";
            public const string PostGame = "PostGame";
            public const string Infrastructure = "Infrastructure";
        }

        public static class Reason
        {
            public const string MissingService = "MissingService";
            public const string MissingAsset = "MissingAsset";
            public const string Timeout = "Timeout";
            public const string Disabled = "Disabled";
            public const string Fallback = "Fallback";
            public const string InvalidConfig = "InvalidConfig";
            public const string Unsupported = "Unsupported";
            public const string Unknown = "Unknown";
        }
    }
}
