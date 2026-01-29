#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Providers
{
    public interface ILevelDefinitionProvider
    {
        bool TryGetDefinition(string levelId, out LevelDefinition definition);
    }
}
