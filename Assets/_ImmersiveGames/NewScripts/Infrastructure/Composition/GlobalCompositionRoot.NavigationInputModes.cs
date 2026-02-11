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
using UnityEngine;
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

            // Comentário:
            // - Sem fallback hardcoded: se o catálogo configurável não existir, é erro de configuração (fail-fast).
            // - Resources path NÃO inclui 'Assets/Resources'. O arquivo deve estar em:
            //   Assets/Resources/<path>.asset
            const string navigationCatalogResourcesPath = "Navigation/GameNavigationCatalog";
            const string sceneRouteCatalogResourcesPath = "SceneFlow/SceneRouteCatalog";
            const string transitionStyleCatalogResourcesPath = "Navigation/TransitionStyleCatalog";
            const string levelCatalogResourcesPath = "Navigation/LevelCatalog";

            var catalogAsset = LoadRequiredResourceAsset<GameNavigationCatalogAsset>(
                navigationCatalogResourcesPath,
                "GameNavigationCatalogAsset");
            var sceneRouteCatalogAsset = LoadRequiredResourceAsset<SceneRouteCatalogAsset>(
                sceneRouteCatalogResourcesPath,
                "SceneRouteCatalogAsset");
            var styleCatalogAsset = LoadRequiredResourceAsset<TransitionStyleCatalogAsset>(
                transitionStyleCatalogResourcesPath,
                "TransitionStyleCatalogAsset");
            var levelCatalogAsset = LoadRequiredResourceAsset<LevelCatalogAsset>(
                levelCatalogResourcesPath,
                "LevelCatalogAsset");

            RegisterGlobalIfMissing<ISceneRouteCatalog>(sceneRouteCatalogAsset, "ISceneRouteCatalog");
            RegisterGlobalIfMissing<ITransitionStyleCatalog>(styleCatalogAsset, "ITransitionStyleCatalog");
            RegisterGlobalIfMissing<ILevelFlowService>(levelCatalogAsset, "ILevelFlowService");

            var sceneRouteResolver = ResolveOrRegisterSceneRouteResolver(sceneRouteCatalogAsset);
            var gameplayProbeResolved = sceneRouteResolver.TryResolve(GameNavigationIntents.ToGameplay, out var gameplayProbeRoute);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] Runtime wiring check: " +
                $"catalogType='{sceneRouteCatalogAsset.GetType().FullName}', " +
                $"resolverType='{sceneRouteResolver.GetType().FullName}', " +
                $"tryResolve('to-gameplay')={gameplayProbeResolved}, " +
                $"resolvedRoute='{(gameplayProbeResolved ? gameplayProbeRoute.RouteKind.ToString() : "<null>")}'.",
                DebugUtility.Colors.Info);

            var service = new GameNavigationService(
                sceneFlow,
                catalogAsset,
                sceneRouteResolver,
                styleCatalogAsset,
                levelCatalogAsset);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] GameNavigationService registrado com catálogos via Resources " +
                $"(navigation='{navigationCatalogResourcesPath}', sceneRoutes='{sceneRouteCatalogResourcesPath}', " +
                $"styles='{transitionStyleCatalogResourcesPath}', levels='{levelCatalogResourcesPath}').",
                DebugUtility.Colors.Info);
        }

        private static ISceneRouteResolver ResolveOrRegisterSceneRouteResolver(ISceneRouteCatalog routeCatalog)
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var existingResolver) && existingResolver != null)
            {
                return existingResolver;
            }

            var resolver = new SceneRouteCatalogResolver(routeCatalog);
            DependencyManager.Provider.RegisterGlobal<ISceneRouteResolver>(resolver);
            return resolver;
        }

        private static T LoadRequiredResourceAsset<T>(string resourcesPath, string assetLabel) where T : ScriptableObject
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[Navigation] ERRO: resourcesPath inválido para {assetLabel}. path='{resourcesPath ?? "<null>"}'.");
                throw new InvalidOperationException($"Resources path inválido para {assetLabel}.");
            }

            try
            {
                var asset = Resources.Load<T>(resourcesPath);
                if (asset != null)
                {
                    return asset;
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[Navigation] ERRO: exceção ao carregar {assetLabel} via Resources. " +
                    $"path='{resourcesPath}', ex='{ex.GetType().Name}: {ex.Message}'.");
                throw;
            }

            var expectedPath = $"Assets/Resources/{resourcesPath}.asset";
            DebugUtility.LogError(typeof(GlobalCompositionRoot),
                $"[Navigation] ERRO: {assetLabel} NÃO encontrado em Resources. path='{resourcesPath}'. " +
                $"Esperado em '{expectedPath}'.");
            throw new InvalidOperationException($"{assetLabel} ausente em Resources (path='{resourcesPath}'). Navegação requer configuração explícita.");
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
