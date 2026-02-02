#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Definitions;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Providers
{
    public interface ILevelDefinitionProvider
    {
        bool TryGetDefinition(string levelId, out LevelDefinition definition);
    }
}
