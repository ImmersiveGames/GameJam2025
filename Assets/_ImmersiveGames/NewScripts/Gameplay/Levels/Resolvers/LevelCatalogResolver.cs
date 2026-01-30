#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers
{
    /// <summary>
    /// Resolver que combina Catalog + Definitions para gerar LevelPlan.
    /// </summary>
    public sealed class LevelCatalogResolver : ILevelCatalogResolver
    {
        private readonly ILevelCatalogProvider _catalogProvider;
        private readonly ILevelDefinitionProvider _definitionProvider;

        public LevelCatalogResolver(ILevelCatalogProvider catalogProvider, ILevelDefinitionProvider definitionProvider)
        {
            _catalogProvider = catalogProvider;
            _definitionProvider = definitionProvider;
        }

        public bool TryResolveInitialLevelId(out string levelId)
        {
            levelId = string.Empty;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver nível inicial.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveInitialLevelId'.");
                return false;
            }

            if (!catalog.TryResolveInitialLevelId(out levelId))
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Nível inicial não configurado no catalog.");
                levelId = string.Empty;
                return false;
            }

            return true;
        }

        public bool TryResolveCatalog(out LevelCatalog catalog)
        {
            catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver catálogo.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveCatalog'.");
                return false;
            }

            return true;
        }

        public bool TryResolveDefinition(string levelId, out LevelDefinition definition)
        {
            definition = null!;

            if (!_definitionProvider.TryGetDefinition(levelId, out definition) || definition == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] LevelDefinition ausente. levelId='{levelId}'.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[OBS][LevelCatalog] DefinitionMissing levelId='{levelId}'.");

                if (_catalogProvider.GetCatalog() == null)
                {
                    DebugUtility.LogWarning<LevelCatalogResolver>(
                        "[OBS][LevelCatalog] CatalogMissing action='ResolveDefinition'.");
                }

                definition = null!;
                return false;
            }

            DebugUtility.Log<LevelCatalogResolver>(
                $"[OBS][LevelCatalog] DefinitionResolved levelId='{definition.LevelId}' contentId='{definition.ContentId}'.");
            return true;
        }

        public bool TryResolveInitialDefinition(out LevelDefinition definition)
        {
            definition = null!;
            if (!TryResolveInitialLevelId(out var levelId))
            {
                return false;
            }

            return TryResolveDefinition(levelId, out definition);
        }

        public bool TryResolveNextLevelId(string levelId, out string nextLevelId)
        {
            nextLevelId = string.Empty;
            var catalog = _catalogProvider.GetCatalog();
            if (catalog == null)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[LevelCatalog] Catalog ausente ao resolver próximo nível.");
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    "[OBS][LevelCatalog] CatalogMissing action='ResolveNextLevelId'.");
                return false;
            }

            if (!catalog.TryResolveNextLevelId(levelId, out nextLevelId))
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] Próximo nível não resolvido. levelId='{levelId}'.");
                nextLevelId = string.Empty;
                return false;
            }

            return true;
        }

        public bool TryResolvePlan(string levelId, out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveDefinition(levelId, out var definition))
            {
                return false;
            }

            plan = definition.ToPlan();
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelCatalogResolver>(
                    $"[LevelCatalog] LevelPlan inválido para levelId='{levelId}'. contentId='{plan.ContentId}'.");
                plan = LevelPlan.None;
                return false;
            }

            options = definition.DefaultOptions.Clone();
            return true;
        }

        public bool TryResolveInitialPlan(out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveInitialLevelId(out var levelId))
            {
                return false;
            }

            return TryResolvePlan(levelId, out plan, out options);
        }

        public bool TryResolveNextPlan(string levelId, out LevelPlan plan, out LevelChangeOptions options)
        {
            plan = LevelPlan.None;
            options = LevelChangeOptions.Default.Clone();

            if (!TryResolveNextLevelId(levelId, out var nextLevelId))
            {
                return false;
            }

            return TryResolvePlan(nextLevelId, out plan, out options);
        }
    }
}
