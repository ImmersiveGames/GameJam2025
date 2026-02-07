// Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManagerInstaller.cs

#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.ContentSwap;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.Levels.Providers;
using _ImmersiveGames.NewScripts.Modules.Levels.Resolvers;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Runtime
{
    /// <summary>
    /// Registro global do módulo de Levels:
    /// - ILevelCatalogProvider (Resources)
    /// - ILevelDefinitionProvider (via catálogo)
    /// - ILevelCatalogResolver
    /// - ILevelManager (aplica ContentSwap)
    /// - ILevelSessionService (seleção/aplicação)
    /// </summary>
    public static class LevelManagerInstaller
    {
        private static bool _registered;

        public static void EnsureRegistered(bool fromBootstrap)
        {
            DebugUtility.LogError(typeof(LevelManagerInstaller),
                "[LevelManager] Levels legacy desativado. Use LevelFlow (LevelCatalogAsset/ILevelFlowService).");
            throw new InvalidOperationException(
                "Levels legacy desativado. Remova LevelManagerInstaller do bootstrap e use LevelFlow.");

            if (_registered)
            {
                return;
            }

            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] DependencyManager.Provider é null; registro abortado.");
                return;
            }

            var provider = DependencyManager.Provider;
            provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
            provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);

            // 1) Provider do catálogo
            if (!provider.TryGetGlobal<ILevelCatalogProvider>(out var catalogProvider) || catalogProvider == null)
            {
                catalogProvider = new ResourcesLevelCatalogProvider();
                provider.RegisterGlobal<ILevelCatalogProvider>(catalogProvider, allowOverride: false);
            }

            // 2) Provider de definições
            if (!provider.TryGetGlobal<ILevelDefinitionProvider>(out var definitionProvider) || definitionProvider == null)
            {
                definitionProvider = new LevelDefinitionProviderFromCatalog(catalogProvider);
                provider.RegisterGlobal<ILevelDefinitionProvider>(definitionProvider, allowOverride: false);
            }

            // 3) Resolver de catálogo
            if (!provider.TryGetGlobal<ILevelCatalogResolver>(out var resolver) || resolver == null)
            {
                resolver = new LevelCatalogResolver(
                    catalogProvider,
                    definitionProvider,
                    runtimeModeProvider,
                    degradedModeReporter);
                provider.RegisterGlobal<ILevelCatalogResolver>(resolver, allowOverride: false);
            }

            // 4) ContentSwap (dependência obrigatória do LevelManager)
            if (!provider.TryGetGlobal<IContentSwapChangeService>(out var contentSwap) || contentSwap == null)
            {
                DebugUtility.LogWarning(typeof(LevelManagerInstaller),
                    "[LevelManager] IContentSwapChangeService não encontrado no DI global; registro abortado.");
                return;
            }

            // 5) LevelManager (construtor real: apenas IContentSwapChangeService)
            if (!provider.TryGetGlobal<ILevelManager>(out var levelManager) || levelManager == null)
            {
                levelManager = new LevelManager(contentSwap);
                provider.RegisterGlobal<ILevelManager>(levelManager, allowOverride: false);
            }

            // 6) Sessão (depende do resolver + manager)
            if (!provider.TryGetGlobal<ILevelSessionService>(out var session) || session == null)
            {
                session = new LevelSessionService(resolver, levelManager);
                provider.RegisterGlobal<ILevelSessionService>(session, allowOverride: false);
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

