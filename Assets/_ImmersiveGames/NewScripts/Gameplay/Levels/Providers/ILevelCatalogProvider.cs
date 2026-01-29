#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Providers
{
    public interface ILevelCatalogProvider
    {
        LevelCatalog? GetCatalog();
    }
}
