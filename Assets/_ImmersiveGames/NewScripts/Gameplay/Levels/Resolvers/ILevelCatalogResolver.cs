#nullable enable

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers
{
    public interface ILevelCatalogResolver
    {
        bool TryResolveInitialLevelId(out string levelId);
        bool TryResolveNextLevelId(string levelId, out string nextLevelId);
        bool TryResolvePlan(string levelId, out LevelPlan plan, out LevelChangeOptions options);
        bool TryResolveInitialPlan(out LevelPlan plan, out LevelChangeOptions options);
        bool TryResolveNextPlan(string levelId, out LevelPlan plan, out LevelChangeOptions options);
    }
}
