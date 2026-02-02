namespace _ImmersiveGames.NewScripts.Runtime.SceneFlow
{
    /// <summary>
    /// Paths canônicos para resolver assets de SceneFlow via Resources.
    ///
    /// Observação:
    /// - O uso de Resources permanece textual, mas a escolha de profile deve ser tipada via <see cref="SceneFlowProfileId"/>.
    /// - Evita espalhar "SceneFlow/Profiles/" no código.
    /// </summary>
    public static class SceneFlowProfilePaths
    {
        public const string ProfilesRoot = "SceneFlow/Profiles";

        private static string For(string profileName)
        {
            string normalized = Normalize(profileName);
            return string.IsNullOrEmpty(normalized) ? string.Empty : $"{ProfilesRoot}/{normalized}";
        }

        public static string For(SceneFlowProfileId profileId)
        {
            return For(profileId.Value);
        }
        private static string Normalize(string value)
        {
            // Mantém normalização local (trim + lower) para paths.
            // A normalização oficial do ID está em SceneFlowProfileId.Normalize.
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        }
    }
}
