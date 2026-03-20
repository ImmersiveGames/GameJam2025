using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static IRestartContextService ResolveOrRegisterRestartContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var existing) && existing != null)
            {
                return existing;
            }

            var service = new RestartContextService();
            DependencyManager.Provider.RegisterGlobal<IRestartContextService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] RestartContextService registrado no DI global.",
                DebugUtility.Colors.Info);

            return service;
        }

        private static void RegisterGameNavigationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] IGameNavigationService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[Navigation] ERRO: ISceneTransitionService indisponivel. Navegacao nao pode ser registrada.");
                throw new InvalidOperationException("IGameNavigationService requer ISceneTransitionService. Verifique o registro do SceneFlow no GlobalCompositionRoot.");
            }

            var bootstrapConfig = GetRequiredBootstrapConfig(out var bootstrapVia);
            var catalogAsset = bootstrapConfig.NavigationCatalog;
            if (catalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required GameNavigationCatalogAsset in BootstrapConfigAsset.navigationCatalog.");
                throw new InvalidOperationException("Missing required BootstrapConfigAsset.navigationCatalog (GameNavigationCatalogAsset).");
            }

            var sceneRouteCatalogAsset = bootstrapConfig.SceneRouteCatalog;
            if (sceneRouteCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required SceneRouteCatalogAsset in BootstrapConfigAsset.sceneRouteCatalog.");
                throw new InvalidOperationException("Missing required BootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] CatalogResolvedVia=BootstrapConfig field=navigationCatalog asset={catalogAsset.name}",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var sceneRouteResolver) || sceneRouteResolver == null)
            {
                throw new InvalidOperationException("ISceneRouteResolver obrigatorio ausente no DI global. Garanta RegisterSceneFlowRoutesRequired no pipeline antes de RegisterGameNavigationService.");
            }

            catalogAsset.GetObservabilitySnapshot(out var rawRoutesCount, out var builtRouteIdsCount, out var hasToGameplay);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] Catalog boot snapshot: " +
                $"bootstrapVia={bootstrapVia}, navigationVia=BootstrapConfig navigationAsset={catalogAsset.name}, stylesVia=DirectRefsOnly, rawRoutesCount={rawRoutesCount}, builtRouteIdsCount={builtRouteIdsCount}, hasToGameplay={hasToGameplay}.",
                DebugUtility.Colors.Info);

            var restartContextService = ResolveOrRegisterRestartContextService();
            var service = new GameNavigationService(
                sceneFlow,
                catalogAsset,
                sceneRouteResolver,
                restartContextService,
                sceneRouteCatalogAsset);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] GameNavigationService registrado. " +
                $"bootstrapVia={bootstrapVia} navigationVia=BootstrapConfig stylesVia=DirectRefsOnly (assets: navigation={catalogAsset.name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterExitToMenuCoordinator()
        {
            RegisterIfMissing(() => new ExitToMenuCoordinator(),
                "[Navigation] ExitToMenuCoordinator ja registrado no DI global.",
                "[Navigation] ExitToMenuCoordinator registrado no DI global.");
        }

        private static void RegisterMacroRestartCoordinator()
        {
            RegisterIfMissing(() => new MacroRestartCoordinator(),
                "[Navigation] MacroRestartCoordinator ja registrado no DI global.",
                "[Navigation] MacroRestartCoordinator registrado no DI global.");
        }

        private static void RegisterLevelSelectedRestartSnapshotBridge()
        {
            RegisterIfMissing(() => new LevelSelectedRestartSnapshotBridge(),
                "[Navigation] LevelSelectedRestartSnapshotBridge ja registrado no DI global.",
                "[Navigation] LevelSelectedRestartSnapshotBridge registrado no DI global.");
        }

        private static void RegisterNavigationLevelRouteBgmBridge()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[Audio][BGM][Bridge] Skipped registration: IAudioBgmService unavailable.");
                return;
            }

            var bootstrap = GetRequiredBootstrapConfig(out _);
            var navigationCatalog = bootstrap.NavigationCatalog;
            var sceneRouteCatalog = bootstrap.SceneRouteCatalog;

            if (navigationCatalog == null || sceneRouteCatalog == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[Audio][BGM][Bridge] Skipped registration: NavigationCatalog or SceneRouteCatalog missing in bootstrap.");
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext);

            RegisterIfMissing(
                () => new NavigationLevelRouteBgmBridge(
                    bgmService,
                    navigationCatalog,
                    sceneRouteCatalog,
                    restartContext),
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge already registered in global DI.",
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge registered in global DI.");
        }
    }
}
