#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Registrador de serviços de LevelManager (providers/resolver/manager/service).
    /// </summary>
    public static class LevelManagerInstaller
    {
        public static void EnsureRegistered(bool fromBootstrap = false)
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] DependencyManager.Provider é null; instalação abortada.");
                return;
            }

            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<ILevelCatalogProvider>(out var catalogProvider) || catalogProvider == null)
            {
                provider.RegisterGlobal<ILevelCatalogProvider>(new ResourcesLevelCatalogProvider(), allowOverride: false);
                provider.TryGetGlobal(out catalogProvider);
            }

            if (catalogProvider == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] ILevelCatalogProvider indisponível; registro abortado.");
                return;
            }

            if (!provider.TryGetGlobal<ILevelDefinitionProvider>(out var definitionProvider) || definitionProvider == null)
            {
                provider.RegisterGlobal<ILevelDefinitionProvider>(new LevelDefinitionProviderFromCatalog(catalogProvider), allowOverride: false);
                provider.TryGetGlobal(out definitionProvider);
            }

            if (definitionProvider == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] ILevelDefinitionProvider indisponível; registro abortado.");
                return;
            }

            if (!provider.TryGetGlobal<ILevelCatalogResolver>(out var resolver) || resolver == null)
            {
                provider.RegisterGlobal<ILevelCatalogResolver>(new LevelCatalogResolver(catalogProvider, definitionProvider), allowOverride: false);
                provider.TryGetGlobal(out resolver);
            }

            if (resolver == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] ILevelCatalogResolver indisponível; registro abortado.");
                return;
            }

            if (!provider.TryGetGlobal<IContentSwapChangeService>(out var contentSwap) || contentSwap == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] IContentSwapChangeService indisponível; ILevelManager não será registrado.");
                return;
            }

            if (!provider.TryGetGlobal<ILevelManager>(out var levelManager) || levelManager == null)
            {
                provider.RegisterGlobal<ILevelManager>(new LevelManager(contentSwap), allowOverride: false);
                provider.TryGetGlobal(out levelManager);
            }

            if (!fromBootstrap)
            {
                DebugUtility.Log(typeof(LevelManagerInstaller),
                    "[LevelManager] Registered (no bootstrap)",
                    DebugUtility.Colors.Info);
            }
        }
    }
}
