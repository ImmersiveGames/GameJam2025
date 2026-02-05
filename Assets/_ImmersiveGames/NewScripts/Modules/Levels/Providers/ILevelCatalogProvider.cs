#nullable enable
using _ImmersiveGames.NewScripts.Modules.Levels.Catalogs;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Providers
{
    public interface ILevelCatalogProvider
    {
        LevelCatalog? GetCatalog();
    }
}
