using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Loading.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Loading.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudService : ILoadingHudService, ILoadingPresentationService
    {
        private const string FeatureName = "loadinghud";
        private const string LoadingHudRootComponentName = nameof(LoadingHudController);
        private const int DefaultResolveFrames = 60;

        private readonly IRuntimeModeProvider _runtime;
        private readonly IDegradedModeReporter _degraded;
        private readonly SceneKeyAsset _loadingHudSceneKey;
        private readonly string _loadingHudSceneName;
        private readonly SemaphoreSlim _ensureGate = new(1, 1);

        private LoadingHudController _controller;
        private bool _disabled;
        private string _lastEnsureLogSignature = string.Empty;
        private int _lastEnsureLogFrame = -1;
        private LoadingProgressSnapshot _lastProgressSnapshot = new(0f, "Loading...");

        public LoadingHudService(
            IRuntimeModeProvider runtimeModeProvider,
            IDegradedModeReporter degradedModeReporter,
            SceneKeyAsset loadingHudSceneKey)
        {
            _runtime = runtimeModeProvider;
            _degraded = degradedModeReporter;
            _loadingHudSceneKey = loadingHudSceneKey;
            _loadingHudSceneName = loadingHudSceneKey != null && !string.IsNullOrWhiteSpace(loadingHudSceneKey.SceneName)
                ? loadingHudSceneKey.SceneName.Trim()
                : string.Empty;
        }

        public Task EnsureReadyAsync(string signature)
        {
            return EnsureLoadedAsync(signature);
        }

        public Task EnsureLoadedAsync(string signature)
        {
            if (_disabled)
            {
                return Task.CompletedTask;
            }

            int currentFrame = Time.frameCount;
            if (SceneFlowSameFrameDedupe.ShouldDedupe(
                    ref _lastEnsureLogFrame,
                    ref _lastEnsureLogSignature,
                    currentFrame,
                    signature))
            {
                DebugUtility.LogVerbose<LoadingHudService>(
                    $"[OBS][Loading] LoadingHudEnsure dedupe_same_frame signature='{signature}' frame={currentFrame}.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogVerbose<LoadingHudService>(
                    $"[LoadingHudEnsure] signature='{signature}'.",
                    DebugUtility.Colors.Info);
            }

            if (string.IsNullOrWhiteSpace(_loadingHudSceneName))
            {
                _disabled = true;
                FailStrict(
                    reason: "scene_key_missing",
                    detail: $"Loading HUD scene key is missing or invalid. keyAsset='{SafeSceneKeyName(_loadingHudSceneKey)}'.",
                    signature: signature);

                return Task.CompletedTask;
            }

            if (!Application.CanStreamedLevelBeLoaded(_loadingHudSceneName))
            {
                _disabled = true;
                FailStrict(
                    reason: "scene_not_in_build",
                    detail: $"Scene '{_loadingHudSceneName}' is not in Build Settings.",
                    signature: signature);

                return Task.CompletedTask;
            }

            return EnsureLoadedInternalAsync(signature);
        }

        public void Show(string signature, string phase)
        {
            Show(signature, phase, null);
        }

        public void Show(string signature, string phase, string message = null)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "Show ignored because the loading root is unavailable.",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            try
            {
                _controller.Show(phase, message);
                _controller.ApplyProgress(_lastProgressSnapshot);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"Show failed: {ex.Message}",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHudShow] signature='{signature}' phase='{phase}' message='{(string.IsNullOrWhiteSpace(message) ? "<default>" : message)}'.",
                DebugUtility.Colors.Info);
        }

        public void Hide(string signature, string phase)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "Hide ignored because the loading root is unavailable.",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            try
            {
                _controller.Hide(phase);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"Hide failed: {ex.Message}",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHudHide] signature='{signature}' phase='{phase}'.",
                DebugUtility.Colors.Info);
        }

        public void SetMessage(string signature, string message, string phase = null)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "SetMessage ignored because the loading root is unavailable.",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            try
            {
                _controller.SetMessage(phase, message);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"SetMessage failed: {ex.Message}",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
            }
        }

        public void SetProgress(string signature, LoadingProgressSnapshot snapshot)
        {
            _lastProgressSnapshot = snapshot;

            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                return;
            }

            try
            {
                _controller.ApplyProgress(snapshot);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"SetProgress failed: {ex.Message}",
                    signature: signature,
                    allowStrictThrow: false);
            }
        }

        private async Task EnsureLoadedInternalAsync(string signature)
        {
            if (IsControllerValid(_controller))
            {
                return;
            }

            await _ensureGate.WaitAsync();
            try
            {
                if (_disabled)
                {
                    return;
                }

                if (IsControllerValid(_controller))
                {
                    return;
                }

                if (!await EnsureSceneLoadedIfNeededAsync(signature))
                {
                    return;
                }

                if (!await TryResolveControllerWithRetriesAsync(signature))
                {
                    FailOrDegrade(
                        reason: "root_not_found",
                        detail: $"No root with {LoadingHudRootComponentName} was found in scene '{_loadingHudSceneName}'.",
                        signature: signature,
                        allowStrictThrow: true);
                }
            }
            catch (Exception ex)
            {
                FailOrDegrade(
                    reason: "exception",
                    detail: $"Failed to ensure loading HUD scene/root. ({ex.Message})",
                    signature: signature,
                    allowStrictThrow: true);
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private async Task<bool> EnsureSceneLoadedIfNeededAsync(string signature)
        {
            var scene = SceneManager.GetSceneByName(_loadingHudSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                return true;
            }

            var loadOp = SceneManager.LoadSceneAsync(_loadingHudSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                FailOrDegrade(
                    reason: "load_op_null",
                    detail: $"LoadSceneAsync returned null for '{_loadingHudSceneName}'.",
                    signature: signature,
                    allowStrictThrow: true);
                return false;
            }

            while (!loadOp.isDone)
            {
                await Task.Yield();
            }

            return true;
        }

        private async Task<bool> TryResolveControllerWithRetriesAsync(string signature)
        {
            for (int i = 0; i < DefaultResolveFrames; i++)
            {
                if (TryResolveController(signature))
                {
                    return true;
                }

                await Task.Yield();
            }

            return IsControllerValid(_controller);
        }

        private bool TryEnsureController(string signature)
        {
            if (IsControllerValid(_controller))
            {
                return true;
            }

            return TryResolveController(signature);
        }

        private bool TryResolveController(string signature)
        {
            var scene = SceneManager.GetSceneByName(_loadingHudSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            LoadingHudController resolved = null;

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var candidate = root.GetComponent<LoadingHudController>();
                if (candidate == null)
                {
                    continue;
                }

                if (resolved != null && resolved != candidate)
                {
                    FailOrDegrade(
                        reason: "multiple_loading_roots",
                        detail: $"Scene '{_loadingHudSceneName}' contains more than one loading root with {LoadingHudRootComponentName}.",
                        signature: signature,
                        allowStrictThrow: true);
                    return false;
                }

                try
                {
                    candidate.EnsureConfiguredOrFail();
                    candidate.ApplyProgress(_lastProgressSnapshot);
                }
                catch (Exception ex)
                {
                    FailOrDegrade(
                        reason: "root_invalid",
                        detail: ex.Message,
                        signature: signature,
                        allowStrictThrow: true);
                    return false;
                }

                resolved = candidate;
            }

            _controller = resolved;
            return IsControllerValid(_controller);
        }

        private static bool IsControllerValid(LoadingHudController controller)
        {
            return controller != null;
        }

        private static string SafeSceneKeyName(SceneKeyAsset sceneKey)
        {
            return sceneKey != null && !string.IsNullOrWhiteSpace(sceneKey.name)
                ? sceneKey.name.Trim()
                : "<missing>";
        }

        private void FailOrDegrade(
            string reason,
            string detail,
            string signature,
            string phase = null,
            bool allowDisable = true,
            bool allowStrictThrow = false)
        {
            if (_runtime != null && _runtime.IsStrict)
            {
                if (allowStrictThrow)
                {
                    FailStrict(reason, detail, signature, phase);
                    return;
                }

                DebugUtility.LogError<LoadingHudService>(
                    $"[LoadingHUD][STRICT] reason='{reason}' signature='{signature}' phase='{phase}'. {detail}");
            }

            if (allowDisable)
            {
                _disabled = true;
            }

            _degraded?.Report(
                feature: FeatureName,
                reason: reason,
                detail: detail,
                signature: signature,
                profile: null);

            DebugUtility.LogWarning<LoadingHudService>(
                $"[LoadingDegraded] reason='{reason}', signature='{signature}', phase='{phase}'. {detail}");
        }

        private static void FailStrict(string reason, string detail, string signature, string phase = null)
        {
            DebugUtility.LogError<LoadingHudService>(
                $"[LoadingHUD][STRICT] reason='{reason}' signature='{signature}' phase='{phase}'. {detail}");

            Debug.Break();

            throw new InvalidOperationException(
                $"LoadingHUD strict failure: {reason}. {detail} (signature='{signature}', phase='{phase}')");
        }
    }
}
