namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Paths canônicos (Resources) para profiles do SceneFlow.
    /// Regra: não montar "SceneFlow/Profiles/..." espalhado no código.
    /// </summary>
    public static class SceneFlowProfilePaths
    {
        public const string ProfilesRoot = "SceneFlow/Profiles";

        public static string For(string profileName)
        {
            var id = SceneFlowProfileNames.Normalize(profileName);
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            return $"{ProfilesRoot}/{id}";
        }
    }
}
