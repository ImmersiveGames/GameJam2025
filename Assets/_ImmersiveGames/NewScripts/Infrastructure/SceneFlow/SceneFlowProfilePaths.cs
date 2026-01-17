namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Paths canônicos para resolver assets de SceneFlow via Resources.
    ///
    /// Observação:
    /// - O uso de Resources permanece textual, mas a escolha de profile deve ser tipada via <see cref="SceneFlowProfileId"/>.
    /// - Evita espalhar "SceneFlow/Profiles/..." no código.
    /// </summary>
    public static class SceneFlowProfilePaths
    {
        public const string ProfilesRoot = "SceneFlow/Profiles";

        public static string For(string profileName)
        {
            var normalized = Normalize(profileName);
            return string.IsNullOrEmpty(normalized) ? string.Empty : $"{ProfilesRoot}/{normalized}";
        }

        public static string For(SceneFlowProfileId profileId)
        {
            return For(profileId.Value);
        }

        /// <summary>
        /// Alias compatível para perfis usados pelo WorldLifecycle/Fade/Loading (mantém intenção sem duplicar strings).
        /// </summary>
        public static string WorldLifecycleProfilePath(string profileName) => For(profileName);

        public static string WorldLifecycleProfilePath(SceneFlowProfileId profileId) => For(profileId);

        public static string Normalize(string value)
        {
            // Mantém normalização local (trim + lower) para paths.
            // A normalização oficial do ID está em SceneFlowProfileId.Normalize.
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}
