// Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManagerInstaller.cs

#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Providers;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Resolvers;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    /// <summary>
    /// Registro global do módulo de Levels:
    /// - ILevelCatalogProvider (Resources)
    /// - ILevelDefinitionProvider (via catálogo)
    /// - ILevelCatalogResolver
    /// - ILevelManager (aplica ContentSwap)
    /// - ILevelSessionService (seleção/aplicação)
    ///
    /// Ajuste Fase A:
    /// - No bootstrap, inicializa a sessão para garantir seleção inicial determinística
    ///   (sem aplicar automaticamente).
    /// </summary>
    public static class LevelManagerInstaller
    {
        private static bool _registered;

        public static void EnsureRegistered(bool fromBootstrap)
        {
            if (_registered)
            {
                return;
            }

            var provider = DependencyManager.Provider;

            // 1) Provider do catálogo
            if (!provider.TryGetGlobal<ILevelCatalogProvider>(out var catalogProvider) || catalogProvider == null)
            {
                catalogProvider = new ResourcesLevelCatalogProvider();
                provider.RegisterGlobal<ILevelCatalogProvider>(catalogProvider);
            }

            // 2) Provider de definições
            if (!provider.TryGetGlobal<ILevelDefinitionProvider>(out var definitionProvider) || definitionProvider == null)
            {
                definitionProvider = new LevelDefinitionProviderFromCatalog(catalogProvider);
                provider.RegisterGlobal<ILevelDefinitionProvider>(definitionProvider);
            }

            // 3) Resolver de catálogo
            if (!provider.TryGetGlobal<ILevelCatalogResolver>(out var resolver) || resolver == null)
            {
                resolver = new LevelCatalogResolver(catalogProvider, definitionProvider);
                provider.RegisterGlobal<ILevelCatalogResolver>(resolver);
            }

            // 4) ContentSwap (dependência obrigatória do LevelManager)
            if (!provider.TryGetGlobal<IContentSwapChangeService>(out var contentSwap) || contentSwap == null)
            {
                DebugUtility.Log(typeof(LevelManagerInstaller),
                    "[LevelManager] IContentSwapChangeService não encontrado no DI global; registro abortado.",
                    DebugUtility.Colors.Warning);
                return;
            }

            // 5) LevelManager (construtor real: apenas IContentSwapChangeService)
            if (!provider.TryGetGlobal<ILevelManager>(out var levelManager) || levelManager == null)
            {
                levelManager = new LevelManager(contentSwap);
                provider.RegisterGlobal<ILevelManager>(levelManager);
            }

            // 6) Sessão (depende do resolver + manager)
            if (!provider.TryGetGlobal<ILevelSessionService>(out var session) || session == null)
            {
                session = new LevelSessionService(resolver, levelManager);
                provider.RegisterGlobal<ILevelSessionService>(session);
            }

            // Ajuste pequeno Fase A:
            // No bootstrap, garantir que exista seleção inicial (sem aplicar).
            if (fromBootstrap)
            {
                var ok = session.Initialize();
                DebugUtility.Log(typeof(LevelManagerInstaller),
                    ok
                        ? "[LevelManager] Session Initialize OK (bootstrap)."
                        : "[LevelManager] Session Initialize falhou (bootstrap).",
                    ok ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            }

            _registered = true;

            // Log somente informativo (não obrigatório)
            if (!fromBootstrap)
            {
                DebugUtility.Log(typeof(LevelManagerInstaller),
                    "[LevelManager] EnsureRegistered chamado fora do bootstrap; verifique a ordem de inicialização.",
                    DebugUtility.Colors.Warning);
            }
            else
            {
                DebugUtility.Log(typeof(LevelManagerInstaller),
                    "[LevelManager] Registro concluído (bootstrap).",
                    DebugUtility.Colors.Success);
            }
        }
    }
}
