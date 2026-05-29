using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.Presentation.Fade.Bindings;
using _ImmersiveGames.NewScripts.Presentation.Fade.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class FadeAdapter : IOperationalFadePort
    {
        private readonly object _sync = new();
        private FadeController _cachedController;
        private string _cachedSceneName = string.Empty;
        private bool _policySourceLogged;

        public async Task<OperationalFadeResult> ExecuteAsync(OperationalFadeRequest request)
        {
            ValidateRequestOrFail(request);

            bool isFadeIn = request.Direction == OperationalFadeDirection.CloseCurtain;
            await ExecuteFadeAsync(request.RouteCommand, isFadeIn);

            string phase = isFadeIn ? "fade_in_completed" : "fade_out_completed";
            string detail = isFadeIn
                ? "Operational curtain close completed."
                : "Operational curtain open completed.";

            return OperationalFadeResult.Completed(
                request.RouteCommand,
                request.Direction,
                phase,
                detail);
        }

        private async Task ExecuteFadeAsync(SessionOperationalRouteCommand command, bool isFadeIn)
        {
            ValidateCommandOrFail(command);

            string sceneName = ResolveFadeSceneNameOrFail();
            FadeController controller = ResolveControllerOrFail(sceneName);
            SceneTransitionProfile transitionProfile = command.TransitionProfile;
            transitionProfile.ValidateOrFail(
                nameof(FadeAdapter),
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

            DebugUtility.Log(typeof(FadeAdapter),
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
                DebugUtility.LogError<FadeAdapter>(message);
                throw new InvalidOperationException(message, ex);
            }

            DebugUtility.Log(typeof(FadeAdapter),
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

            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail();

            string sceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(
                RuntimePersistentSceneRole.Fade,
                nameof(FadeAdapter));

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

        private static void ValidateRequestOrFail(OperationalFadeRequest request)
        {
            if (!request.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] OperationalFadeRequest invalido.");
            }
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

        private RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail()
        {
            if (RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) && snapshot != null)
            {
                IRuntimePolicyConfigGroupReadOnly runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                RuntimePersistentScenesPolicyAsset registryPolicy = runtimePolicy.RuntimePersistentScenesPolicy;
                string policyValidationError = string.Empty;
                bool registryPolicyValid = registryPolicy != null && registryPolicy.TryValidate(out policyValidationError);
                if (!registryPolicyValid)
                {
                    throw new InvalidOperationException($"[FATAL][Config][SessionOperationalFade] RuntimeConfigRegistry invariant breach: RuntimePersistentScenesPolicyAsset ausente/invalido no snapshot. detail='{policyValidationError}'.");
                }

                LogPolicySourceOnce(registryPolicy);
                return registryPolicy;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionOperationalFade] RuntimeConfigRegistry snapshot obrigatorio ausente para RuntimePersistentScenesPolicy migrado.");
        }

        private void LogPolicySourceOnce(RuntimePersistentScenesPolicyAsset policy)
        {
            if (_policySourceLogged)
            {
                return;
            }

            _policySourceLogged = true;

            DebugUtility.Log(typeof(FadeAdapter),
                $"[OBS][RuntimePolicy][Config] FadeAdapter using RuntimeConfigRegistry persistentScenesPolicy. policyId='{policy.PolicyId}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildContextSignature(SessionOperationalRouteCommand command, string phase)
        {
            return $"{command.RouteIdentity}|{command.RouteOperationId}|{command.TransitionId}|{command.RouteSequence}|{phase}|{command.TransitionMode}|{command.TransitionProfileLabel}|{command.Source}|{command.Reason}";
        }
    }
}
