using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.InputModes;
using _ImmersiveGames.NewScripts.Modules.InputModes.Interop;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // Navigation / InputMode
        // --------------------------------------------------------------------

        private static void RegisterInputModesFromRuntimeConfig()
        {
            if (!DependencyManager.HasInstance)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[InputMode] DependencyManager indisponível. Registro do IInputModeService ignorado.");
                ReportInputModesDegraded("missing_dependency_manager",
                    "DependencyManager not available during global composition.");
                return;
            }

            var provider = DependencyManager.Provider;

            provider.TryGetGlobal<RuntimeModeConfig>(out var config);
            var settings = config != null ? config.inputModes : null;

            bool enableInputModes = settings?.enableInputModes ?? true;
            bool logVerbose = settings?.logVerbose ?? true;

            if (!enableInputModes)
            {
                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[InputMode] InputModes desabilitado via RuntimeModeConfig; IInputModeService não será registrado.",
                        DebugUtility.Colors.Info);
                }

                ReportInputModesDegraded("disabled_by_config",
                    "InputModes disabled by RuntimeModeConfig.");
                return;
            }

            if (provider.TryGetGlobal<IInputModeService>(out var existing) && existing != null)
            {
                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[InputMode] IInputModeService já registrado no DI global.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            string playerMapName = settings?.playerActionMapName;
            string menuMapName = settings?.menuActionMapName;

            if (string.IsNullOrWhiteSpace(playerMapName))
            {
                playerMapName = "Player";
            }

            if (string.IsNullOrWhiteSpace(menuMapName))
            {
                menuMapName = "UI";
            }

            try
            {
                provider.RegisterGlobal<IInputModeService>(new InputModeService(playerMapName, menuMapName));

                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[InputMode] IInputModeService registrado no DI global (playerMap='{playerMapName}', menuMap='{menuMapName}').",
                        DebugUtility.Colors.Info);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[InputMode] Falha ao registrar IInputModeService. ex='{ex.GetType().Name}: {ex.Message}'.");
                ReportInputModesDegraded("register_failed", ex.Message);
            }
        }

        private static void ReportInputModesDegraded(string reason, string detail)
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var reporter) || reporter == null)
            {
                return;
            }

            reporter.Report(DegradedKeys.Feature.InputModes, reason, detail);
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

            var levelCatalogAsset = bootstrapConfig.LevelCatalog;
            if (levelCatalogAsset == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] Missing required LevelCatalogAsset in NewScriptsBootstrapConfigAsset.levelCatalog.");
                throw new InvalidOperationException(
                    "Missing required NewScriptsBootstrapConfigAsset.levelCatalog (LevelCatalogAsset).");
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
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] CatalogResolvedVia={catalogsVia} field=levelCatalog asset={levelCatalogAsset.name}",
                DebugUtility.Colors.Info);

            RegisterGlobalIfMissing<ITransitionStyleCatalog>(styleCatalogAsset, "ITransitionStyleCatalog");
            RegisterGlobalIfMissing<ILevelFlowService>(levelCatalogAsset, "ILevelFlowService");

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
                $"levelsVia={catalogsVia} levelsAsset={levelCatalogAsset.name}, " +
                $"rawRoutesCount={rawRoutesCount}, " +
                $"builtRouteIdsCount={builtRouteIdsCount}, " +
                $"hasToGameplay={hasToGameplay}.",
                DebugUtility.Colors.Info);

            var service = new GameNavigationService(
                sceneFlow,
                catalogAsset,
                sceneRouteResolver,
                styleCatalogAsset,
                levelCatalogAsset,
                intentCatalogAsset);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] GameNavigationService registrado. " +
                $"bootstrapVia={bootstrapVia} navigationVia={catalogsVia} intentsVia={catalogsVia} stylesVia={catalogsVia} levelsVia={catalogsVia} " +
                $"(assets: navigation={catalogAsset.name}, intents={intentCatalogAsset.name}, styles={styleCatalogAsset.name}, levels={levelCatalogAsset.name}).",
                DebugUtility.Colors.Info);


            if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var levelFlowRuntime) || levelFlowRuntime == null)
            {
                levelFlowRuntime = new LevelFlowRuntimeService(levelCatalogAsset, service, catalogAsset, styleCatalogAsset);
                DependencyManager.Provider.RegisterGlobal<ILevelFlowRuntimeService>(levelFlowRuntime);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[OBS][LevelFlow] LevelFlowRuntimeService registrado (trilho canônico StartGameplayAsync(string,...)).",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterGlobalIfMissing<T>(T service, string label) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[Navigation] {label} já registrado no DI global. Registro ignorado.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal(service);
        }

        private static void RegisterExitToMenuNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<ExitToMenuNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] ExitToMenuNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new ExitToMenuNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] ExitToMenuNavigationBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRestartNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<RestartNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] RestartNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new RestartNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] RestartNavigationBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterInputModeSceneFlowBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<SceneFlowInputModeBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new SceneFlowInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[InputMode] SceneFlowInputModeBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

    }
}
