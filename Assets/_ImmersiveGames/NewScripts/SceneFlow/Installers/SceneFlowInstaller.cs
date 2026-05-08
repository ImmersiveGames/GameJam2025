using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Policies;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SceneFlow.Installers
{
    /// <summary>
    /// Installer do SceneFlow.
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
            RegisterTransitionCompletionGate(bootstrapConfig);
            EnsureRouteActorSetRefContext();

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
                () => new SceneFlowNavigationPolicy(),
                "[SceneFlow] INavigationPolicy ja registrado no DI global.",
                "[SceneFlow] INavigationPolicy registrado no DI global (SceneFlowNavigationPolicy).");
        }

        private static void RegisterRouteGuard()
        {
            RegisterIfMissing<IRouteGuard>(
                () => new SceneFlowRouteGuard(),
                "[SceneFlow] IRouteGuard ja registrado no DI global.",
                "[SceneFlow] IRouteGuard registrado no DI global (SceneFlowRouteGuard).");
        }

        private static void RegisterRouteResetPolicy()
        {
            RegisterIfMissing<IRouteResetPolicy>(
                () => new SceneRouteResetPolicy(),
                "[SceneFlow] IRouteResetPolicy ja registrado no DI global.",
                "[SceneFlow] IRouteResetPolicy registrado no DI global (SceneRouteResetPolicy).");
        }

        private static void RegisterTransitionCompletionGate(BootstrapConfigAsset bootstrapConfig)
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                return;
            }

            if (ResolveCompositionProfile(bootstrapConfig) != CompositionProfileKind.Base11Sandbox)
            {
                return;
            }

            RegisterIfMissing<ISceneTransitionCompletionGate>(
                () => new Base11SandboxTransitionCompletionGate(),
                "[SceneFlow] ISceneTransitionCompletionGate ja registrado no DI global.",
                "[SceneFlow] ISceneTransitionCompletionGate registrado no DI global (Base11SandboxTransitionCompletionGate).");
        }

        public static void EnsureRouteActorSetRefContext()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var existingContext) && existingContext != null)
            {
                return;
            }

            var service = new SceneFlowRouteActorSetRefService();
            DependencyManager.Provider.RegisterGlobal<ISceneFlowRouteActorSetRefContext>(service);
            DependencyManager.Provider.RegisterGlobal(service);

            DebugUtility.Log(typeof(SceneFlowInstaller),
                "[OBS][ActorsExecution] Route actor-set context composed during SceneFlow installer phase.",
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

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string successMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SceneFlowInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal(factory());
            DebugUtility.LogVerbose(typeof(SceneFlowInstaller), successMessage, DebugUtility.Colors.Info);
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
                    $"[FATAL][Config][Fade] Fade scene '{fadeSceneName}' not found in Build Settings. bootstrap='{bootstrapAssetName}', keyAsset='{fadeSceneKeyAssetName}'.";
                return false;
            }

            return true;
        }

        private static CompositionProfileKind ResolveCompositionProfile(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SceneFlow] BootstrapConfigAsset obrigatorio ausente para resolver CompositionProfile.");
            }

            RuntimeModeConfig runtimeModeConfig = bootstrapConfig.RuntimeModeConfig;
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SceneFlow] RuntimeModeConfig obrigatorio ausente no BootstrapConfigAsset para resolver CompositionProfile.");
            }

            return runtimeModeConfig.compositionProfile;
        }
    }
}
