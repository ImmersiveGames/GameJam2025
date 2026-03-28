using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Bootstrap
{
    /// <summary>
    /// Installer do SceneFlow.
    ///
    /// Responsabilidade:
    /// - registrar contratos, servicos, policies, guards e configs do modulo;
    /// - nao compor runtime nem ativar bridges/event handlers.
    /// </summary>
    public static class SceneFlowInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SceneFlow] BootstrapConfigAsset obrigatorio ausente para instalar o SceneFlow.");
            }

            RegisterFadeService(bootstrapConfig);
            RegisterSceneFlowSignatureCache();
            RegisterNavigationPolicy();
            RegisterRouteGuard();
            RegisterRouteResetPolicy();
            RegisterLoadingServices(bootstrapConfig);

            _installed = true;

            DebugUtility.Log(typeof(SceneFlowInstaller),
                "[SceneFlow] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterFadeService(BootstrapConfigAsset bootstrapConfig)
        {
            string fadeSceneName = ResolveFadeSceneName(bootstrapConfig, out string failureReason);
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                if (ShouldDegradeFadeInRuntime())
                {
                    DebugUtility.LogError(typeof(SceneFlowInstaller),
                        $"[ERROR][DEGRADED][Fade] {failureReason}");

                    DependencyManager.Provider.RegisterGlobal<IFadeService>(
                        new DegradedFadeService(failureReason),
                        allowOverride: true);
                    return;
                }

                throw new InvalidOperationException(failureReason);
            }

            RegisterIfMissing<IFadeService>(
                () => new FadeService(fadeSceneName),
                "[Fade] IFadeService ja registrado no DI global.",
                $"[Fade] IFadeService registrado no DI global (scene='{fadeSceneName}').");
        }

        private static void RegisterSceneFlowSignatureCache()
        {
            RegisterIfMissing<ISceneFlowSignatureCache>(
                () => new SceneFlowSignatureCache(),
                "[SceneFlow] ISceneFlowSignatureCache ja registrado no DI global.",
                "[SceneFlow] SceneFlowSignatureCache registrado no DI global.");
        }

        private static void RegisterNavigationPolicy()
        {
            RegisterIfMissing<INavigationPolicy>(
                () => new AllowAllNavigationPolicy(),
                "[SceneFlow] INavigationPolicy ja registrado no DI global.",
                "[SceneFlow] INavigationPolicy registrado no DI global (AllowAllNavigationPolicy).");
        }

        private static void RegisterRouteGuard()
        {
            RegisterIfMissing<IRouteGuard>(
                () => new AllowAllRouteGuard(),
                "[SceneFlow] IRouteGuard ja registrado no DI global.",
                "[SceneFlow] IRouteGuard registrado no DI global (AllowAllRouteGuard).");
        }

        private static void RegisterRouteResetPolicy()
        {
            RegisterIfMissing<IRouteResetPolicy>(
                () => new SceneRouteResetPolicy(),
                "[SceneFlow] IRouteResetPolicy ja registrado no DI global.",
                "[SceneFlow] IRouteResetPolicy registrado no DI global (SceneRouteResetPolicy).");
        }

        private static void RegisterLoadingServices(BootstrapConfigAsset bootstrapConfig)
        {
            var provider = DependencyManager.Provider;
            bool hasPresentation = provider.TryGetGlobal<ILoadingPresentationService>(out var existingPresentation) && existingPresentation != null;
            bool hasHud = provider.TryGetGlobal<ILoadingHudService>(out var existingHud) && existingHud != null;

            if (hasPresentation && hasHud)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowInstaller),
                    "[Loading] ILoadingPresentationService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (hasPresentation && !hasHud)
            {
                if (existingPresentation is ILoadingHudService loadingHudService)
                {
                    provider.RegisterGlobal(loadingHudService);
                    DebugUtility.LogVerbose(typeof(SceneFlowInstaller),
                        "[Loading] ILoadingHudService registrado como alias do servico existente.",
                        DebugUtility.Colors.Info);
                    return;
                }

                throw new InvalidOperationException(
                    $"[FATAL][Config][Loading] ILoadingPresentationService existente nao implementa ILoadingHudService (tipo='{existingPresentation.GetType().Name}').");
            }

            if (hasHud && !hasPresentation)
            {
                if (existingHud is ILoadingPresentationService loadingPresentationService)
                {
                    provider.RegisterGlobal(loadingPresentationService);
                    DebugUtility.LogVerbose(typeof(SceneFlowInstaller),
                        "[Loading] ILoadingPresentationService registrado como alias do servico existente.",
                        DebugUtility.Colors.Info);
                    return;
                }

                throw new InvalidOperationException(
                    $"[FATAL][Config][Loading] ILoadingHudService existente nao implementa ILoadingPresentationService (tipo='{existingHud.GetType().Name}').");
            }

            var runtimeMode = ResolveRequiredRuntimeModeProvider();
            var degradedReporter = ResolveDegradedReporterIfNeeded(runtimeMode);
            var loadingHudSceneKey = bootstrapConfig.LoadingHudSceneKey;
            if (loadingHudSceneKey == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Loading] Missing required BootstrapConfigAsset.loadingHudSceneKey.");
            }

            var concreteService = new LoadingHudService(runtimeMode, degradedReporter, loadingHudSceneKey);
            DependencyManager.Provider.RegisterGlobal<ILoadingPresentationService>(concreteService);
            DependencyManager.Provider.RegisterGlobal<ILoadingHudService>(concreteService);

            DebugUtility.LogVerbose(typeof(SceneFlowInstaller),
                "[Loading] ILoadingPresentationService e ILoadingHudService registrados no DI global.",
                DebugUtility.Colors.Info);
        }

        private static IRuntimeModeProvider ResolveRequiredRuntimeModeProvider()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeMode) && runtimeMode != null)
            {
                return runtimeMode;
            }

            throw new InvalidOperationException("[FATAL][Config][Loading] IRuntimeModeProvider obrigatorio ausente no DI global.");
        }

        private static IDegradedModeReporter ResolveDegradedReporterIfNeeded(IRuntimeModeProvider runtimeMode)
        {
            if (runtimeMode != null && !runtimeMode.IsStrict)
            {
                if (DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter) && degradedReporter != null)
                {
                    return degradedReporter;
                }

                return null;
            }

            if (DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var strictReporter) && strictReporter != null)
            {
                return strictReporter;
            }

            throw new InvalidOperationException("[FATAL][Config][Loading] IDegradedModeReporter obrigatorio ausente em modo strict.");
        }

        private static bool ShouldDegradeFadeInRuntime()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider) && runtimeModeProvider != null)
            {
                return !runtimeModeProvider.IsStrict;
            }

            return false;
        }

        private static string ResolveFadeSceneName(BootstrapConfigAsset bootstrap, out string failureReason)
        {
            var fadeSceneKey = bootstrap.FadeSceneKey;
            if (fadeSceneKey == null)
            {
                failureReason = $"[FATAL][Config][Fade] Missing fadeSceneKey. asset='{bootstrap.name}', field='fadeSceneKey'.";
                return string.Empty;
            }

            string fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                failureReason =
                    $"[FATAL][Config][Fade] Invalid fadeSceneKey SceneName. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}'.";
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

            if (!Application.CanStreamedLevelBeLoaded(fadeSceneName))
            {
                failureReason =
                    $"[FATAL][Config][Fade] FadeScene ausente no Build Settings. asset='{bootstrapAssetName}', field='fadeSceneKey', keyAsset='{fadeSceneKeyAssetName}', scene='{fadeSceneName}'.";
                return false;
            }

            return true;
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(SceneFlowInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
