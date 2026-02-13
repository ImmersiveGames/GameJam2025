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
            const string transitionStyleCatalogResourcesPath = "Navigation/TransitionStyleCatalog";
            const string levelCatalogResourcesPath = "Navigation/LevelCatalog";

            TryResolveBootstrapConfig(out var bootstrapConfig, out _);

            GameNavigationCatalogAsset catalogAsset;
            bool navigationFromLegacyResources = false;
            string navigationVia;
            if (bootstrapConfig != null && bootstrapConfig.NavigationCatalog != null)
            {
                catalogAsset = bootstrapConfig.NavigationCatalog;
                navigationVia = "Bootstrap";
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=Bootstrap field=navigationCatalog asset={catalogAsset.name}",
                    DebugUtility.Colors.Info);
            }
            else
            {
                navigationFromLegacyResources = true;
                navigationVia = "LegacyResources";
                catalogAsset = LoadRequiredResourceAsset<GameNavigationCatalogAsset>(
                    navigationCatalogResourcesPath,
                    "GameNavigationCatalogAsset");
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=LegacyResources path={navigationCatalogResourcesPath} asset={catalogAsset.name}",
                    DebugUtility.Colors.Info);
            }

            TransitionStyleCatalogAsset styleCatalogAsset;
            bool styleFromLegacyResources = false;
            string stylesVia;
            if (bootstrapConfig != null && bootstrapConfig.TransitionStyleCatalog != null)
            {
                styleCatalogAsset = bootstrapConfig.TransitionStyleCatalog;
                stylesVia = "Bootstrap";
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=Bootstrap field=transitionStyleCatalog asset={styleCatalogAsset.name}",
                    DebugUtility.Colors.Info);
            }
            else
            {
                styleFromLegacyResources = true;
                stylesVia = "LegacyResources";
                styleCatalogAsset = LoadRequiredResourceAsset<TransitionStyleCatalogAsset>(
                    transitionStyleCatalogResourcesPath,
                    "TransitionStyleCatalogAsset");
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=LegacyResources path={transitionStyleCatalogResourcesPath} asset={styleCatalogAsset.name}",
                    DebugUtility.Colors.Info);
            }

            LevelCatalogAsset levelCatalogAsset;
            bool levelFromLegacyResources = false;
            string levelsVia;
            if (bootstrapConfig != null && bootstrapConfig.LevelCatalog != null)
            {
                levelCatalogAsset = bootstrapConfig.LevelCatalog;
                levelsVia = "Bootstrap";
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=Bootstrap field=levelCatalog asset={levelCatalogAsset.name}",
                    DebugUtility.Colors.Info);
            }
            else
            {
                levelFromLegacyResources = true;
                levelsVia = "LegacyResources";
                levelCatalogAsset = LoadRequiredResourceAsset<LevelCatalogAsset>(
                    levelCatalogResourcesPath,
                    "LevelCatalogAsset");
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] CatalogResolvedVia=LegacyResources path={levelCatalogResourcesPath} asset={levelCatalogAsset.name}",
                    DebugUtility.Colors.Info);
            }

            if (navigationFromLegacyResources)
            {
                LogPotentialDuplicateResourcesAsset(navigationCatalogResourcesPath, catalogAsset, "GameNavigationCatalogAsset");
            }

            if (styleFromLegacyResources)
            {
                LogPotentialDuplicateResourcesAsset(transitionStyleCatalogResourcesPath, styleCatalogAsset, "TransitionStyleCatalogAsset");
            }

            if (levelFromLegacyResources)
            {
                LogPotentialDuplicateResourcesAsset(levelCatalogResourcesPath, levelCatalogAsset, "LevelCatalogAsset");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            levelCatalogAsset.GetLegacySceneDataStats(out int dirtyLegacyCount, out int totalLevels);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] LevelDefinitions legacy scene data: " +
                $"dirty={dirtyLegacyCount} / total={totalLevels}. " +
                "Use Tools/NewScripts/Navigation/Clear Legacy Scene Data in LevelDefinitions para limpar.",
                DebugUtility.Colors.Info);
#endif

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
                $"navigationVia={navigationVia} navigationAsset={catalogAsset.name}, " +
                $"stylesVia={stylesVia} stylesAsset={styleCatalogAsset.name}, " +
                $"levelsVia={levelsVia} levelsAsset={levelCatalogAsset.name}, " +
                $"rawRoutesCount={rawRoutesCount}, " +
                $"builtRouteIdsCount={builtRouteIdsCount}, " +
                $"hasToGameplay={hasToGameplay}.",
                DebugUtility.Colors.Info);

            var service = new GameNavigationService(
                sceneFlow,
                catalogAsset,
                sceneRouteResolver,
                styleCatalogAsset,
                levelCatalogAsset);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] GameNavigationService registrado. " +
                $"navigationVia={navigationVia} stylesVia={stylesVia} levelsVia={levelsVia} " +
                $"(assets: navigation={catalogAsset.name}, styles={styleCatalogAsset.name}, levels={levelCatalogAsset.name}).",
                DebugUtility.Colors.Info);
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


        private static void LogPotentialDuplicateResourcesAsset<T>(
            string canonicalResourcesPath,
            T canonicalAsset,
            string assetLabel)
            where T : ScriptableObject
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (canonicalAsset == null)
            {
                return;
            }

            var allAssetsOfType = Resources.LoadAll<T>(string.Empty);
            if (allAssetsOfType == null || allAssetsOfType.Length <= 1)
            {
                return;
            }

            int sameNameCount = 0;
            for (int i = 0; i < allAssetsOfType.Length; i++)
            {
                if (allAssetsOfType[i] == null)
                {
                    continue;
                }

                if (string.Equals(allAssetsOfType[i].name, canonicalAsset.name, StringComparison.Ordinal))
                {
                    sameNameCount++;
                }
            }

            if (sameNameCount <= 1)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                "[OBS][Navigation] Possível duplicata de catálogo em Resources detectada. " +
                $"assetLabel='{assetLabel}', canonicalPath='{canonicalResourcesPath}', assetName='{canonicalAsset.name}', " +
                $"sameNameCount={sameNameCount}, totalAssetsOfType={allAssetsOfType.Length}. " +
                "Mantenha apenas um catálogo canônico por nome para evitar ambiguidade de setup.");
#endif
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
