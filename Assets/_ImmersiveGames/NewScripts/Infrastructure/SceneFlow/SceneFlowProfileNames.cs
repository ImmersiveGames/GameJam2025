namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Nomes canônicos de profiles usados pelo SceneFlow (Fade/WorldLifecycle/Logs).
    /// Centraliza strings para evitar drift.
    /// </summary>
    public static class SceneFlowProfileNames
    {
        public const string Startup = "startup";
        public const string Frontend = "frontend";
        public const string Gameplay = "gameplay";
    }
}
