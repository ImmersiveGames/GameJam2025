using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Presentation.Loading.Bindings;
using _ImmersiveGames.NewScripts.Presentation.Loading.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class SessionActivityTransitionLoadingAdapter : ISessionActivityTransitionLoadingAdapter
    {
        private readonly object _sync = new();
        private LoadingHudController _cachedController;
        private string _cachedSceneName = string.Empty;
        private bool _isVisible;
        private float _visibleSinceRealtime;

        public Task StartAsync(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution, string source, string reason)
        {
            ValidateInputOrFail(identity, resolution);
            LoadingHudController controller = ResolveLoadingControllerOrFail();
            controller.Show("ActivityTransitionLoading", "Loading...");
            controller.ApplyProgress(new LoadingProgressSnapshot(0f, "Loading..."));

            lock (_sync)
            {
                _isVisible = true;
                _visibleSinceRealtime = Time.realtimeSinceStartup;
            }

            _ = source;
            _ = reason;
            return Task.CompletedTask;
        }

        public async Task ReportProgressAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            float normalizedProgress,
            string stepLabel,
            string message,
            string source,
            string reason)
        {
            ValidateInputOrFail(identity, resolution);
            LoadingHudController controller = ResolveLoadingControllerOrFail();
            LoadingProgressSnapshot snapshot = new(normalizedProgress, stepLabel, message);
            controller.ApplyProgress(snapshot);

            if (normalizedProgress >= 0.999f)
            {
                string signature = $"{identity.CycleSignature}|activity_transition_loading_progress|{source}|{reason}";
                await controller.ApplyProgressAndSettleAsync(snapshot, signature);
            }
        }

        public Task CompleteAsync(
            SessionActivityIdentity identity,
            SessionActivityTransitionResolution resolution,
            string source,
            string reason)
        {
            return ReportProgressAsync(
                identity,
                resolution,
                1f,
                "RevealSafePoint",
                "Activity transition loading complete.",
                source,
                reason);
        }

        public async Task HideAsync(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution, string source, string reason)
        {
            ValidateInputOrFail(identity, resolution);
            LoadingHudController controller = ResolveLoadingControllerOrFail();
            RuntimeLoadingProfileAsset profile = resolution.LoadingProfile;

            await WaitMinimumVisibleAsync(profile);
            controller.Hide("ActivityTransitionLoading");
            await EnsureVisualHideSettledAsync(controller, $"{identity.CycleSignature}|activity_transition_loading_hide|{source}|{reason}");

            lock (_sync)
            {
                _isVisible = false;
            }
        }

        private static void ValidateInputOrFail(SessionActivityIdentity identity, SessionActivityTransitionResolution resolution)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityTransitionLoadingAdapter requires valid identity.");
            }

            if (!resolution.HasLoadingProfile || resolution.LoadingProfile == null)
            {
                throw new InvalidOperationException("SessionActivityTransitionLoadingAdapter requires resolved loading profile.");
            }

            if (!resolution.LoadingProfile.TryValidate(out string error))
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] loadingProfile invalido. detail='{error}'.");
            }
        }

        private LoadingHudController ResolveLoadingControllerOrFail()
        {
            lock (_sync)
            {
                if (_cachedController != null && !string.IsNullOrWhiteSpace(_cachedSceneName))
                {
                    return _cachedController;
                }
            }

            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = ResolvePersistentScenesPolicyOrFail();
            string sceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(RuntimePersistentSceneRole.Loading, nameof(SessionActivityTransitionLoadingAdapter));
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] LoadingScene obrigatoria nao esta carregada. scene='{sceneName}'.");
            }

            LoadingHudController controller = FindControllerInSceneOrFail(scene, sceneName);
            lock (_sync)
            {
                _cachedSceneName = sceneName;
                _cachedController = controller;
            }

            return controller;
        }

        private static LoadingHudController FindControllerInSceneOrFail(Scene scene, string sceneName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            LoadingHudController resolved = null;

            for (int i = 0; i < roots.Length; i++)
            {
                LoadingHudController candidate = roots[i].GetComponentInChildren<LoadingHudController>(true);
                if (candidate == null)
                {
                    continue;
                }

                if (resolved != null && resolved != candidate)
                {
                    throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] LoadingScene contains more than one LoadingHudController. scene='{sceneName}'.");
                }

                candidate.EnsureConfiguredOrFail();
                resolved = candidate;
            }

            if (resolved == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] LoadingHudController ausente na LoadingScene obrigatoria. scene='{sceneName}'.");
            }

            return resolved;
        }

        private static RuntimePersistentScenesPolicyAsset ResolvePersistentScenesPolicyOrFail()
        {
            if (!RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityTransitionLoading] RuntimeConfigRegistry snapshot obrigatorio ausente.");
            }

            IRuntimePolicyConfigGroupReadOnly runtimePolicy = snapshot.RuntimePolicy;
            if (runtimePolicy == null || runtimePolicy.RuntimePersistentScenesPolicy == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityTransitionLoading] RuntimePersistentScenesPolicy obrigatoria ausente.");
            }

            return runtimePolicy.RuntimePersistentScenesPolicy;
        }

        private async Task WaitMinimumVisibleAsync(RuntimeLoadingProfileAsset profile)
        {
            if (profile.MinimumVisibleSeconds <= 0f)
            {
                return;
            }

            float startedAt;
            lock (_sync)
            {
                startedAt = _visibleSinceRealtime;
            }

            if (startedAt <= 0f)
            {
                return;
            }

            while (Time.realtimeSinceStartup - startedAt < profile.MinimumVisibleSeconds)
            {
                await Task.Yield();
            }
        }

        private static async Task EnsureVisualHideSettledAsync(LoadingHudController controller, string signature)
        {
            int startFrame = Time.frameCount;
            await Task.Yield();
            while (Time.frameCount == startFrame)
            {
                await Task.Yield();
            }

            CanvasGroup rootGroup = controller.RootGroup;
            if (rootGroup == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] LoadingHudController rootGroup ausente after hide. contextSignature='{signature}'.");
            }

            if (rootGroup.alpha > 0f)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityTransitionLoading] LoadingHudController hide did not settle visually. alpha='{rootGroup.alpha:0.###}' contextSignature='{signature}'.");
            }
        }
    }
}
