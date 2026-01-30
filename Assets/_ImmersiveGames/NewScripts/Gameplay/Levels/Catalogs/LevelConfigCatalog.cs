using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs
{
    /// <summary>
    /// Catálogo de configuração (ADR-0017): extensão leve do LevelCatalog.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelConfigCatalog",
        menuName = "ImmersiveGames/Levels/Level Config Catalog",
        order = 2)]
    public sealed class LevelConfigCatalog : LevelCatalog
    {
    }
}
