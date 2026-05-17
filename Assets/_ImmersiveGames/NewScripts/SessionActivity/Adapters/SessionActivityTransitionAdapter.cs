using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.Presentation.Fade.Bindings;
using _ImmersiveGames.NewScripts.Presentation.Fade.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class SessionActivityTransitionAdapter : ISessionActivityTransitionAdapter
    {
        private readonly object _sync = new();
        private FadeController _cachedController;
        private string _cachedFadeSceneName = string.Empty;

        public void CloseCurtain(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution, string source, string reason)
        {
            Execute(identity, resolution, source, reason, close: true);
        }

        public void OpenCurtain(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution, string source, string reason)
        {
            Execute(identity, resolution, source, reason, close: false);
        }

        private void Execute(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution, string source, string reason, bool close)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityTransitionAdapter requires valid identity.");
            }

            if (resolution.Mode != ActivityTransitionMode.CutWithCurtain)
            {
                throw new InvalidOperationException($"SessionActivityTransitionAdapter only supports CutWithCurtain mode. mode='{resolution.Mode}'.");
            }

            if (!resolution.HasFadeProfile)
            {
                throw new InvalidOperationException("SessionActivityTransitionAdapter requires resolved fade profile for CutWithCurtain.");
            }

            resolution.FadeProfile.ValidateOrFail(nameof(SessionActivityTransitionAdapter), close ? "close_curtain" : "open_curtain");
            FadeController controller = ResolveFadeControllerOrFail();
            FadeConfig fadeConfig = new(
                resolution.FadeProfile.FadeInDuration,
                resolution.FadeProfile.FadeOutDuration,
                resolution.FadeProfile.FadeInCurve,
                resolution.FadeProfile.FadeOutCurve);
            controller.Configure(fadeConfig);
            string signature = $"{identity.CycleSignature}|{(close ? "activity_transition_close" : "activity_transition_open")}|{source}|{reason}";
            controller.SetContextSignature(signature);

            if (close)
            {
                controller.FadeInAsync(signature).GetAwaiter().GetResult();
                return;
            }

            controller.FadeOutAsync(signature).GetAwaiter().GetResult();
        }

        private FadeController ResolveFadeControllerOrFail()
        {
            lock (_sync)
            {
                if (_cachedController != null && !string.IsNullOrWhiteSpace(_cachedFadeSceneName))
                {
                    return _cachedController;
                }
            }

            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail();
            string fadeSceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(RuntimePersistentSceneRole.Fade, nameof(SessionActivityTransitionAdapter));
            Scene scene = SceneManager.GetSceneByName(fadeSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransition] FadeScene obrigatoria nao esta carregada. scene='{fadeSceneName}'.");
            }

            FadeController controller = FindControllerInSceneOrFail(scene, fadeSceneName);
            lock (_sync)
            {
                _cachedFadeSceneName = fadeSceneName;
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

            throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransition] FadeController ausente na FadeScene obrigatoria. scene='{sceneName}'.");
        }

        private static RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail()
        {
            if (!RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityTransition] RuntimeConfigRegistry snapshot obrigatorio ausente.");
            }

            IRuntimePolicyConfigGroupReadOnly runtimePolicy = snapshot.RuntimePolicy;
            if (runtimePolicy == null || runtimePolicy.RuntimePersistentScenesPolicy == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityTransition] RuntimePersistentScenesPolicy obrigatoria ausente.");
            }

            return runtimePolicy.RuntimePersistentScenesPolicy;
        }
    }
}
