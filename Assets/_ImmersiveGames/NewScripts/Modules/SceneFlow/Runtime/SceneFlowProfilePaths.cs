namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    public static class SceneFlowProfilePaths
    {
        public const string ProfilesRoot = "SceneFlow/Profiles";

        public static string For(SceneFlowProfileId profileId) => For(profileId, ProfilesRoot);

        public static string For(SceneFlowProfileId profileId, string basePath)
        {
            if (!profileId.IsValid)
                return string.Empty;

            var root = NormalizeBasePath(basePath);
            var id = profileId.Value; // already normalized by SceneFlowProfileId

            if (string.IsNullOrWhiteSpace(root))
                return id;

            return $"{root}/{id}";
        }

        public static string For(string profileName) => For(new SceneFlowProfileId(profileName), ProfilesRoot);

        public static string For(string profileName, string basePath) => For(new SceneFlowProfileId(profileName), basePath);

        private static string NormalizeBasePath(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                return ProfilesRoot;

            return basePath.Trim().TrimEnd('/');
        }
    }
}
