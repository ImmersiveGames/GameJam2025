using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallSceneFlowServices()
        {
            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();
            RegisterSceneFlowRoutesRequired();
            RegisterSceneFlowNative();
            RegisterSceneFlowSignatureCache();
            RegisterSceneFlowRouteResetPolicy();

            // ADR-0010: mantem o Loading no final da instalacao do SceneFlow
            // para preservar o ponto de registro equivalente do pipeline.
            RegisterSceneFlowLoadingIfAvailable();
        }

        private static void RegisterSceneFlowNative()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] SceneTransitionService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Loader/Fade (NewScripts standalone)
            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);

            // Gate composto: (1) WorldLifecycle reset -> (2) LevelPrepare (macro gameplay) -> libera FadeOut.
            ISceneTransitionCompletionGate completionGate = null;
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                completionGate = existingGate;
            }

            if (completionGate is not MacroLevelPrepareCompletionGate)
            {
                WorldLifecycleResetCompletionGate innerGate = completionGate as WorldLifecycleResetCompletionGate;
                if (innerGate == null)
                {
                    if (completionGate != null)
                    {
                        DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                            $"[SceneFlow] ISceneTransitionCompletionGate não é WorldLifecycleResetCompletionGate (tipo='{completionGate.GetType().Name}'). Substituindo para cumprir o contrato SceneFlow/WorldLifecycle (completion gate).");
                    }

                    innerGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                }

                completionGate = new MacroLevelPrepareCompletionGate(innerGate);
                DependencyManager.Provider.RegisterGlobal(completionGate);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[SceneFlow] ISceneTransitionCompletionGate registrado ({completionGate.GetType().Name}, inner={innerGate.GetType().Name}).",
                    DebugUtility.Colors.Info);
            }

            INavigationPolicy navigationPolicy = null;
            if (DependencyManager.Provider.TryGetGlobal<INavigationPolicy>(out var existingPolicy) && existingPolicy != null)
            {
                navigationPolicy = existingPolicy;
            }
            else
            {
                navigationPolicy = new AllowAllNavigationPolicy();
                DependencyManager.Provider.RegisterGlobal(navigationPolicy);
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] INavigationPolicy registrado (AllowAllNavigationPolicy).",
                    DebugUtility.Colors.Info);
            }

            IRouteGuard routeGuard = null;
            if (DependencyManager.Provider.TryGetGlobal<IRouteGuard>(out var existingRouteGuard) && existingRouteGuard != null)
            {
                routeGuard = existingRouteGuard;
            }
            else
            {
                routeGuard = new AllowAllRouteGuard();
                DependencyManager.Provider.RegisterGlobal(routeGuard);
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] IRouteGuard registrado (AllowAllRouteGuard).",
                    DebugUtility.Colors.Info);
            }

            var routeResolver = ResolveOrRegisterRouteResolverRequired();

            var service = new SceneTransitionService(loaderAdapter, fadeAdapter, completionGate, navigationPolicy, routeResolver, routeGuard);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] SceneTransitionService nativo registrado (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}, Policy={navigationPolicy.GetType().Name}, RouteResolver={(routeResolver == null ? "None" : routeResolver.GetType().Name)}, RouteGuard={routeGuard.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowSignatureCache()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] ISceneFlowSignatureCache já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ISceneFlowSignatureCache>(
                new SceneFlowSignatureCache());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] SceneFlowSignatureCache registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowRouteResetPolicy()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRouteResetPolicy>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] IRouteResetPolicy já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var routeResolver = ResolveOrRegisterRouteResolverRequired();

            DependencyManager.Provider.RegisterGlobal<IRouteResetPolicy>(
                new SceneRouteResetPolicy(routeResolver));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] IRouteResetPolicy registrado (SceneRouteResetPolicy).",
                DebugUtility.Colors.Info);
        }

        private static ISceneRouteResolver ResolveOrRegisterRouteResolverRequired()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var existingResolver) && existingResolver != null)
            {
                return existingResolver;
            }
            throw new InvalidOperationException(
                "[SceneFlow] ISceneRouteResolver obrigatório ausente no DI global. " +
                "Garanta a execução de RegisterSceneFlowRoutesRequired no pipeline antes de RegisterSceneFlowNative.");
        }

        private static void RegisterSceneFlowFadeModule()
        {
            var bootstrap = GetRequiredBootstrapConfig(out _);
            var fadeSceneName = TryResolveFadeSceneName(bootstrap, out var failureReason);

            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                HandleFadeBootstrapFailure(failureReason);
                return;
            }

            RegisterIfMissing<IFadeService>(() => new FadeService(fadeSceneName));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[Fade] IFadeService registered in global DI (scene='{fadeSceneName}').",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) && fadeService != null)
            {
                _ = PreloadFadeSceneAsync(fadeService, fadeSceneName);
            }
            else
            {
                HandleFadeBootstrapFailure("IFadeService could not be resolved after registration.");
            }
        }

        private static string TryResolveFadeSceneName(NewScriptsBootstrapConfigAsset bootstrap, out string failureReason)
        {
            var fadeSceneKey = bootstrap.FadeSceneKey;
            if (fadeSceneKey == null)
            {
                failureReason = $"Missing fadeSceneKey. asset='{bootstrap.name}', field='fadeSceneKey'.";
                return string.Empty;
            }

            var fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                failureReason =
                    $"Invalid fadeSceneKey SceneName. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}'.";
                return string.Empty;
            }

            if (!TryValidateFadeSceneInBuildSettings(fadeSceneName, bootstrap.name, fadeSceneKey.name, out failureReason))
            {
                return string.Empty;
            }

            failureReason = string.Empty;
            return fadeSceneName;
        }

        private static bool TryValidateFadeSceneInBuildSettings(
            string fadeSceneName,
            string bootstrapAssetName,
            string fadeSceneKeyAssetName,
            out string failureReason)
        {
            failureReason = string.Empty;
            string buildScenePath = TryResolveBuildSettingsScenePath(fadeSceneName);
            if (!string.IsNullOrWhiteSpace(buildScenePath))
            {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(buildScenePath);
                if (buildIndex < 0)
                {
                    failureReason =
                        $"FadeScene inválida no Build Settings: path='{buildScenePath}' retornou buildIndex={buildIndex}. " +
                        $"Corrija em File > Build Settings e garanta a cena habilitada. asset='{bootstrapAssetName}', field='fadeSceneKey', keyAsset='{fadeSceneKeyAssetName}', scene='{fadeSceneName}'.";
                    return false;
                }

                return true;
            }

            if (!Application.CanStreamedLevelBeLoaded(fadeSceneName))
            {
                failureReason =
                    $"FadeScene ausente no Build Settings. Adicione/ative a cena em File > Build Settings (scene/path) para fade funcionar. " +
                    $"asset='{bootstrapAssetName}', field='fadeSceneKey', keyAsset='{fadeSceneKeyAssetName}', scene='{fadeSceneName}'.";
                return false;
            }

            return true;
        }

        private static string TryResolveBuildSettingsScenePath(string fadeSceneName)
        {
            string scenePath = string.Empty;
            TryResolveBuildSettingsScenePathEditor(fadeSceneName, ref scenePath);
            return scenePath ?? string.Empty;
        }

        private static async System.Threading.Tasks.Task PreloadFadeSceneAsync(IFadeService fadeService, string fadeSceneName)
        {
            try
            {
                await fadeService.EnsureReadyAsync();
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Fade] FadeScene ready (source=GlobalCompositionRoot/Preload, scene='{fadeSceneName}').",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                HandleFadeRuntimeFailure(
                    $"Failed to preload FadeScene '{fadeSceneName}'. ex='{ex.GetType().Name}: {ex.Message}'",
                    "fade_preload_failed");
            }
        }

        private static void HandleFadeBootstrapFailure(string reason)
        {
            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[ERROR][DEGRADED][Fade] {reason}");

                DependencyManager.Provider.RegisterGlobal<IFadeService>(
                    new DegradedFadeService(reason),
                    allowOverride: true);
                return;
            }

            HandleFadeRuntimeFailure(reason, "fade_bootstrap_invalid", isConfigFatal: true);
        }

        private static void HandleFadeRuntimeFailure(string reason, string degradedReason, bool isConfigFatal = false)
        {
            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[ERROR][DEGRADED][Fade] {reason}");

                DependencyManager.Provider.RegisterGlobal<IFadeService>(
                    new DegradedFadeService(degradedReason),
                    allowOverride: true);
                return;
            }

            var tag = isConfigFatal ? "[FATAL][Config][Fade]" : "[FATAL][Fade]";
            DebugUtility.LogError(typeof(GlobalCompositionRoot), $"{tag} {reason}");

            StopPlayModeOrQuit();

            throw new InvalidOperationException($"{tag} {reason}");
        }

        static partial void TryResolveBuildSettingsScenePathEditor(string fadeSceneName, ref string scenePath);

        private static bool ShouldDegradeFadeInRuntime()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }

        private static void RegisterSceneFlowLoadingIfAvailable()
        {
            // ADR-0010: LoadingHudService depende da policy Strict/Release + reporter de degraded.
            // Mantemos best-effort: se por algum motivo os serviços não estiverem disponíveis,
            // ainda assim injetamos nulls e deixamos o próprio serviço decidir como degradar.
            DependencyManager.Provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeMode);
            DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            if (!DependencyManager.Provider.TryGetGlobal<ILoadingPresentationService>(out var presentationService) || presentationService == null)
            {
                var concreteService = new LoadingHudService(runtimeMode, degradedReporter);
                DependencyManager.Provider.RegisterGlobal<ILoadingPresentationService>(concreteService);
                DependencyManager.Provider.RegisterGlobal<ILoadingHudService>(concreteService);
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Loading] ILoadingPresentationService e ILoadingHudService registrados no DI global.",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Loading] LoadingHudOrchestrator já registrado no DI global.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                RegisterIfMissing(() => new LoadingHudOrchestrator());

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Loading] LoadingHudOrchestrator registrado no DI global.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<LoadingProgressOrchestrator>(out var existingProgress) || existingProgress == null)
            {
                RegisterIfMissing(() => new LoadingProgressOrchestrator());

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Loading] LoadingProgressOrchestrator registrado no DI global.",
                    DebugUtility.Colors.Info);
            }
        }
    }
}


