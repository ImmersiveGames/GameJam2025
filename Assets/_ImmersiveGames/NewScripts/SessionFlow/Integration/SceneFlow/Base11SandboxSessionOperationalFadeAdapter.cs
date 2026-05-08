using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Bindings;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxSessionOperationalFadeAdapter : ISessionOperationalFadeAdapter
    {
        private readonly object _sync = new();
        private FadeController _cachedController;
        private string _cachedSceneName = string.Empty;

        public async Task FadeInAsync(SessionOperationalRouteCommand command)
        {
            await ExecuteFadeAsync(command, isFadeIn: true);
        }

        public async Task FadeOutAsync(SessionOperationalRouteCommand command)
        {
            await ExecuteFadeAsync(command, isFadeIn: false);
        }

        private async Task ExecuteFadeAsync(SessionOperationalRouteCommand command, bool isFadeIn)
        {
            ValidateCommandOrFail(command);

            string sceneName = ResolveFadeSceneNameOrFail();
            FadeController controller = ResolveControllerOrFail(sceneName);
            SceneTransitionProfile transitionProfile = command.TransitionProfile;
            transitionProfile.ValidateOrFail(
                nameof(Base11SandboxSessionOperationalFadeAdapter),
                $"{(isFadeIn ? "fadeIn" : "fadeOut")} routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeIdentity='{command.RouteIdentity}' routeSequence='{command.RouteSequence}'.");

            FadeConfig fadeConfig = new(
                transitionProfile.FadeInDuration,
                transitionProfile.FadeOutDuration,
                transitionProfile.FadeInCurve,
                transitionProfile.FadeOutCurve);

            string phase = isFadeIn ? "fadeIn" : "fadeOut";
            string signature = BuildContextSignature(command, phase);

            controller.SetContextSignature(signature);
            controller.Configure(fadeConfig);

            DebugUtility.Log(typeof(Base11SandboxSessionOperationalFadeAdapter),
                $"[OBS][SessionOperationalFade][Adapter] {phase}Started routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{command.Source}' reason='{command.Reason}' fadeScene='{sceneName}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            try
            {
                if (isFadeIn)
                {
                    await controller.FadeInAsync(signature);
                }
                else
                {
                    await controller.FadeOutAsync(signature);
                }
            }
            catch (Exception ex)
            {
                string message = $"[FATAL][Fade][SessionOperationalPipeline] {phase} failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' transitionProfile='{command.TransitionProfileLabel}' fadeScene='{sceneName}' exceptionType='{ex.GetType().Name}' exceptionMessage='{ex.Message}'.";
                DebugUtility.LogError<Base11SandboxSessionOperationalFadeAdapter>(message);
                throw new InvalidOperationException(message, ex);
            }

            DebugUtility.Log(typeof(Base11SandboxSessionOperationalFadeAdapter),
                $"[OBS][SessionOperationalFade][Adapter] {phase}Completed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{command.Source}' reason='{command.Reason}' fadeScene='{sceneName}' contextSignature='{signature}'.",
                DebugUtility.Colors.Success);
        }

        private string ResolveFadeSceneNameOrFail()
        {
            lock (_sync)
            {
                if (_cachedController != null && !string.IsNullOrWhiteSpace(_cachedSceneName))
                {
                    return _cachedSceneName;
                }
            }

            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = runtimeModeConfig.RuntimePersistentScenesPolicy;
            if (persistentScenesPolicy == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] RuntimePersistentScenesPolicyAsset obrigatorio ausente para transitionMode=Profile.");
            }

            string sceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(
                RuntimePersistentSceneRole.Fade,
                nameof(Base11SandboxSessionOperationalFadeAdapter));

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] FadeScene obrigatoria nao esta carregada. scene='{sceneName}'.");
            }

            FadeController controller = FindControllerInSceneOrFail(scene, sceneName);

            lock (_sync)
            {
                _cachedSceneName = sceneName;
                _cachedController = controller;
            }

            return sceneName;
        }

        private FadeController ResolveControllerOrFail(string sceneName)
        {
            lock (_sync)
            {
                if (_cachedController != null)
                {
                    return _cachedController;
                }
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] FadeScene obrigatoria nao esta carregada. scene='{sceneName}'.");
            }

            FadeController controller = FindControllerInSceneOrFail(scene, sceneName);

            lock (_sync)
            {
                _cachedSceneName = sceneName;
                _cachedController = controller;
            }

            return controller;
        }

        private static FadeController FindControllerInSceneOrFail(Scene scene, string sceneName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                FadeController controller = roots[i].GetComponentInChildren<FadeController>(true);
                if (controller != null)
                {
                    return controller;
                }
            }

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] FadeController ausente na FadeScene obrigatoria. scene='{sceneName}'.");
        }

        private static void ValidateCommandOrFail(SessionOperationalRouteCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] SessionOperationalRouteCommand invalido.");
            }

            if (!command.UsesTransition)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] transitionMode invalido para o adapter de fade.");
            }

            if (command.TransitionProfile == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] transitionProfile obrigatorio ausente.");
            }

            if (!command.TransitionProfile.TryValidate(out string errorMessage))
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] transitionProfile invalido. detail='{errorMessage}'.");
            }
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            if (!DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) || runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] RuntimeModeConfig obrigatorio ausente para resolver a FadeScene persistente.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] compositionProfile invalido para o adapter de fade. compositionProfile='{runtimeModeConfig.compositionProfile}'.");
            }

            return runtimeModeConfig;
        }

        private static string BuildContextSignature(SessionOperationalRouteCommand command, string phase)
        {
            return $"{command.RouteIdentity}|{command.RouteOperationId}|{command.TransitionId}|{command.RouteSequence}|{phase}|{command.TransitionMode}|{command.TransitionProfileLabel}|{command.Source}|{command.Reason}";
        }
    }
}
