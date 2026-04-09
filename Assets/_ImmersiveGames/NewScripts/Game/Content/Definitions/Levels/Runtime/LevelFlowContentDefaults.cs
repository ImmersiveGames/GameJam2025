namespace _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime
{
    public static class LevelFlowContentDefaults
    {
        public const string DefaultContentId = "content.default";

        public static string Normalize(string contentId)
        {
            return string.IsNullOrWhiteSpace(contentId) ? DefaultContentId : contentId.Trim();
        }

    }
}
