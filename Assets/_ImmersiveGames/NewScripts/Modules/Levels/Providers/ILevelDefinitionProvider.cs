#nullable enable
using _ImmersiveGames.NewScripts.Modules.Levels.Definitions;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Providers
{
    public interface ILevelDefinitionProvider
    {
        bool TryGetDefinition(string levelId, out LevelDefinition definition);
    }
}
