using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public static class LevelFlowContentDefaults
    {
        public const string DefaultContentId = "content.default";

        public static string Normalize(string contentId)
        {
            return string.IsNullOrWhiteSpace(contentId) ? DefaultContentId : contentId.Trim();
        }

        public static string Normalize(string contentId, LevelDefinitionAsset levelRef)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                return contentId.Trim();
            }

            return levelRef != null ? BuildCanonicalLevelContentId(levelRef) : DefaultContentId;
        }

        public static string BuildCanonicalLevelContentId(LevelDefinitionAsset levelRef)
        {
            return levelRef == null
                ? DefaultContentId
                : $"level-content:{levelRef.name}";
        }
    }
}
