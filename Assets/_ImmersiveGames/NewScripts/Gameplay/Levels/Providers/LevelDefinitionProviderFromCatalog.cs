#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Providers
{
    /// <summary>
    /// Provider de LevelDefinition que busca dentro do LevelCatalog configurado.
    /// </summary>
    public sealed class LevelDefinitionProviderFromCatalog : ILevelDefinitionProvider
    {
        private readonly ILevelCatalogProvider _catalogProvider;

        public LevelDefinitionProviderFromCatalog(ILevelCatalogProvider catalogProvider)
        {
            _catalogProvider = catalogProvider;
        }

        public bool TryGetDefinition(string levelId, out LevelDefinition definition)
        {
            definition = null!;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelDefinitionProviderFromCatalog>(
                    "[LevelCatalog] LevelCatalog indisponível ao resolver LevelDefinition.");
                return false;
            }

            if (!catalog.TryGetDefinition(levelId, out definition))
            {
                DebugUtility.LogWarning<LevelDefinitionProviderFromCatalog>(
                    $"[LevelCatalog] LevelDefinition não encontrada. levelId='{levelId}'.");
                definition = null!;
                return false;
            }

            return true;
        }
    }
}
