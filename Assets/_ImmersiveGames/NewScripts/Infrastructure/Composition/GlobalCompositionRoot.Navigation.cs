using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
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
                    "[Navigation] IGameNavigationService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                // Comentário: Navegação depende do SceneFlow; sem ele, o build está inconsistente.
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[Navigation] ERRO: ISceneTransitionService indisponível. Navegação não pode ser registrada.");
                throw new InvalidOperationException("IGameNavigationService requer ISceneTransitionService. Verifique o registro do SceneFlow no GlobalCompositionRoot.");
            }

            var bootstrapConfig = GetRequiredBootstrapConfig(out var bootstrapVia);

            var catalogAsset = bootstrapConfig.NavigationCatalog;
            if (catalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required GameNavigationCatalogAsset in NewScriptsBootstrapConfigAsset.navigationCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.navigationCatalog (GameNavigationCatalogAsset).");
            }

            var intentCatalogAsset = bootstrapConfig.NavigationIntentCatalog;
            if (intentCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required GameNavigationIntentCatalogAsset in NewScriptsBootstrapConfigAsset.navigationIntentCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.navigationIntentCatalog (GameNavigationIntentCatalogAsset).");
            }

            var styleCatalogAsset = bootstrapConfig.TransitionStyleCatalog;
            if (styleCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required TransitionStyleCatalogAsset in NewScriptsBootstrapConfigAsset.transitionStyleCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.transitionStyleCatalog (TransitionStyleCatalogAsset).");
            }
            var sceneRouteCatalogAsset = bootstrapConfig.SceneRouteCatalog;
            if (sceneRouteCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required SceneRouteCatalogAsset in NewScriptsBootstrapConfigAsset.sceneRouteCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.sceneRouteCatalog (SceneRouteCatalogAsset).");
            }
            const string catalogsVia = "BootstrapConfig";
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] CatalogResolvedVia={catalogsVia} field=navigationCatalog asset={catalogAsset.name}",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] CatalogResolvedVia={catalogsVia} field=navigationIntentCatalog asset={intentCatalogAsset.name}",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] CatalogResolvedVia={catalogsVia} field=transitionStyleCatalog asset={styleCatalogAsset.name}",
                DebugUtility.Colors.Info);

            RegisterIfMissing<ITransitionStyleCatalog>(
                () => styleCatalogAsset,
                "[Navigation] ITransitionStyleCatalog ja registrado no DI global. Registro ignorado.",
                null);

            if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var sceneRouteResolver) || sceneRouteResolver == null)
            {
                throw new InvalidOperationException("ISceneRouteResolver obrigatório ausente no DI global. Garanta RegisterSceneFlowRoutesRequired no pipeline antes de RegisterGameNavigationService.");
            }

            catalogAsset.GetObservabilitySnapshot(
                out var rawRoutesCount,
                out var builtRouteIdsCount,
                out var hasToGameplay);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] Catalog boot snapshot: " +
                $"bootstrapVia={bootstrapVia}, " +
                $"navigationVia={catalogsVia} navigationAsset={catalogAsset.name}, " +
                $"intentsVia={catalogsVia} intentsAsset={intentCatalogAsset.name}, " +
                $"stylesVia={catalogsVia} stylesAsset={styleCatalogAsset.name}, " +
                $"rawRoutesCount={rawRoutesCount}, " +
                $"builtRouteIdsCount={builtRouteIdsCount}, " +
                $"hasToGameplay={hasToGameplay}.",
                DebugUtility.Colors.Info);

            var restartContextService = ResolveOrRegisterRestartContextService();

            var service = new GameNavigationService(
                sceneFlow,
                catalogAsset,
                sceneRouteResolver,
                styleCatalogAsset,
                intentCatalogAsset,
                restartContextService,
                sceneRouteCatalogAsset);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] GameNavigationService registrado. " +
                $"bootstrapVia={bootstrapVia} navigationVia={catalogsVia} intentsVia={catalogsVia} stylesVia={catalogsVia} " +
                $"(assets: navigation={catalogAsset.name}, intents={intentCatalogAsset.name}, styles={styleCatalogAsset.name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterExitToMenuNavigationBridge()
        {
            RegisterIfMissing(
                () => new ExitToMenuNavigationBridge(),
                "[Navigation] ExitToMenuNavigationBridge ja registrado no DI global.",
                "[Navigation] ExitToMenuNavigationBridge registrado no DI global.");
        }

        private static void RegisterMacroRestartCoordinator()
        {
            RegisterIfMissing(
                () => new MacroRestartCoordinator(),
                "[Navigation] MacroRestartCoordinator ja registrado no DI global.",
                "[Navigation] MacroRestartCoordinator registrado no DI global.");
        }

        private static void RegisterLevelSelectedRestartSnapshotBridge()
        {
            RegisterIfMissing(
                () => new LevelSelectedRestartSnapshotBridge(),
                "[Navigation] LevelSelectedRestartSnapshotBridge ja registrado no DI global.",
                "[Navigation] LevelSelectedRestartSnapshotBridge registrado no DI global.");
        }
    }
}
