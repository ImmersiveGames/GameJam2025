#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Catalogs;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Providers
{
    public interface ILevelCatalogProvider
    {
        LevelCatalog? GetCatalog();
    }
}
