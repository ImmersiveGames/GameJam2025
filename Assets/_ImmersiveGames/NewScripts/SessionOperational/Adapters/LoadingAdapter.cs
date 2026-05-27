using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Presentation.Loading.Bindings;
using _ImmersiveGames.NewScripts.Presentation.Loading.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingAdapter : ILoadingAdapter
    {
        private readonly object _sync = new();
        private LoadingHudController _cachedController;
        private string _cachedSceneName = string.Empty;
        private bool _isVisible;
        private float _visibleSinceRealtime;

        public Task ShowLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact)
        {
            ValidateCommandAndFactOrFail(command, fact, SessionOperationalLoadingStage.LoadingStarted);

            LoadingHudController controller = ResolveControllerOrFail(command);
            string signature = BuildSignature(command);

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] showStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' scene='{command.LoadingSceneName}' stage='{fact.Stage}' showImmediately='{command.ShowImmediately}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            ApplySnapshot(controller, fact, showIfNeeded: command.ShowImmediately);

            lock (_sync)
            {
                if (command.ShowImmediately)
                {
                    _isVisible = true;
                    _visibleSinceRealtime = Time.realtimeSinceStartup;
                }
            }

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] showCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' scene='{command.LoadingSceneName}' stage='{fact.Stage}' progress='{fact.NormalizedProgress:0.###}' contextSignature='{signature}'.",
                DebugUtility.Colors.Success);

            return Task.CompletedTask;
        }

        public Task UpdateLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact)
        {
            ValidateCommandAndFactOrFail(command, fact, fact.Stage);

            LoadingHudController controller = ResolveControllerOrFail(command);
            string signature = BuildSignature(command);
            LoadingProgressSnapshot snapshot = BuildSnapshot(fact);

            lock (_sync)
            {
                if (!_isVisible)
                {
                    controller.Show(fact.StepLabel, fact.Message);
                    _isVisible = true;
                    _visibleSinceRealtime = Time.realtimeSinceStartup;
                }
            }

            if (IsFinalProgress(fact))
            {
                return UpdateFinalProgressAsync(command, fact, controller, snapshot, signature);
            }

            controller.ApplyProgress(snapshot);

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] progressApplied routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' progress='{fact.NormalizedProgress:0.###}' step='{fact.StepLabel}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            return Task.CompletedTask;
        }

        public async Task HideLoadingAsync(SessionOperationalLoadingCommand command, SessionOperationalLoadingFact fact)
        {
            ValidateCommandAndFactOrFail(command, fact, fact.Stage);

            LoadingHudController controller = ResolveControllerOrFail(command);
            string signature = BuildSignature(command);
            bool forceHide = fact.OutcomeKind == SessionOperationalLoadingOutcomeKind.Failed;

            if (!command.HideAfterCompletion && !forceHide)
            {
                DebugUtility.Log(typeof(LoadingAdapter),
                    $"[OBS][SessionOperationalLoading][Adapter] hideSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' hideAfterCompletion='false' contextSignature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] hideStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' scene='{command.LoadingSceneName}' stage='{fact.Stage}' forceHide='{forceHide}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            await WaitMinimumVisibleTimeAsync(command);

            lock (_sync)
            {
                if (_isVisible)
                {
                    controller.Hide(fact.StepLabel);
                    _isVisible = false;
                }
            }

            await EnsureVisualHideSettledAsync(controller, signature);

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] hideCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' scene='{command.LoadingSceneName}' stage='{fact.Stage}' contextSignature='{signature}'.",
                DebugUtility.Colors.Success);
        }

        private static void ValidateCommandAndFactOrFail(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingFact fact,
            SessionOperationalLoadingStage expectedStage)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalLoading] SessionOperationalLoadingCommand invalido.");
            }

            if (!fact.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalLoading] SessionOperationalLoadingFact invalido.");
            }

            if (expectedStage != SessionOperationalLoadingStage.Unknown && fact.Stage != expectedStage)
            {
                // Stage mismatch is not fatal for updates, but show requires LoadingStarted.
                if (expectedStage == SessionOperationalLoadingStage.LoadingStarted)
                {
                    throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] show stage invalido. expected='{expectedStage}' actual='{fact.Stage}'.");
                }
            }
        }

        private static string BuildSignature(SessionOperationalLoadingCommand command)
        {
            return $"{command.RouteIdentity}|{command.RouteOperationId}|{command.TransitionId}|{command.RouteSequence}|{command.LoadingProfileId}";
        }

        private LoadingHudController ResolveControllerOrFail(SessionOperationalLoadingCommand command)
        {
            if (!string.IsNullOrWhiteSpace(_cachedSceneName) &&
                string.Equals(_cachedSceneName, command.LoadingSceneName, StringComparison.Ordinal) &&
                IsControllerValid(_cachedController))
            {
                return _cachedController;
            }

            Scene scene = SceneManager.GetSceneByName(command.LoadingSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] LoadingHudScene obrigatoria nao esta carregada. scene='{command.LoadingSceneName}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            LoadingHudController controller = FindControllerInSceneOrFail(scene, command);

            lock (_sync)
            {
                _cachedSceneName = command.LoadingSceneName;
                _cachedController = controller;
            }

            return controller;
        }

        private static LoadingHudController FindControllerInSceneOrFail(Scene scene, SessionOperationalLoadingCommand command)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            LoadingHudController resolved = null;

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                LoadingHudController candidate = root.GetComponentInChildren<LoadingHudController>(true);
                if (candidate == null)
                {
                    continue;
                }

                if (resolved != null && resolved != candidate)
                {
                    throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] LoadingHudScene contains more than one LoadingHudController. scene='{scene.name}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
                }

                candidate.EnsureConfiguredOrFail();
                resolved = candidate;
            }

            if (resolved == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] LoadingHudController ausente na LoadingHudScene obrigatoria. scene='{scene.name}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            return resolved;
        }

        private static bool IsControllerValid(LoadingHudController controller)
        {
            return controller != null;
        }

        private static LoadingProgressSnapshot BuildSnapshot(SessionOperationalLoadingFact fact)
        {
            return new LoadingProgressSnapshot(
                fact.NormalizedProgress,
                fact.StepLabel,
                fact.Message);
        }

        private static void ApplySnapshot(LoadingHudController controller, SessionOperationalLoadingFact fact, bool showIfNeeded)
        {
            if (controller == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalLoading] LoadingHudController obrigatorio ausente.");
            }

            LoadingProgressSnapshot snapshot = BuildSnapshot(fact);

            if (showIfNeeded)
            {
                controller.Show(fact.StepLabel, fact.Message);
            }
            else
            {
                controller.SetMessage(fact.StepLabel, fact.Message);
            }

            controller.ApplyProgress(snapshot);
        }

        private async Task UpdateFinalProgressAsync(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingFact fact,
            LoadingHudController controller,
            LoadingProgressSnapshot snapshot,
            string signature)
        {
            ApplySnapshot(controller, fact, showIfNeeded: true);

            await controller.ApplyProgressAndSettleAsync(snapshot, signature);

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] progressApplied routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' progress='{fact.NormalizedProgress:0.###}' step='{fact.StepLabel}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] progressVisualSettled routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' progress='{fact.NormalizedProgress:0.###}' step='{fact.StepLabel}' contextSignature='{signature}'.",
                DebugUtility.Colors.Success);

            await WaitFinalProgressHoldAsync(command, fact, signature);
        }

        private static async Task WaitFinalProgressHoldAsync(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingFact fact,
            string signature)
        {
            float holdSeconds = command.FinalProgressHoldSeconds;

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] finalProgressHoldStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingProfile='{command.LoadingProfileId}' finalProgressHoldSeconds='{holdSeconds:0.###}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' contextSignature='{signature}'.",
                DebugUtility.Colors.Info);

            if (holdSeconds > 0f)
            {
                float startedAt = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - startedAt < holdSeconds)
                {
                    await Task.Yield();
                }
            }

            DebugUtility.Log(typeof(LoadingAdapter),
                $"[OBS][SessionOperationalLoading][Adapter] finalProgressHoldCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingProfile='{command.LoadingProfileId}' finalProgressHoldSeconds='{holdSeconds:0.###}' source='{command.Source}' reason='{command.Reason}' stage='{fact.Stage}' contextSignature='{signature}'.",
                DebugUtility.Colors.Success);
        }

        private static bool IsFinalProgress(SessionOperationalLoadingFact fact)
        {
            return fact.NormalizedProgress >= 0.999f;
        }

        private async Task WaitMinimumVisibleTimeAsync(SessionOperationalLoadingCommand command)
        {
            float minimumVisibleSeconds = command.MinimumVisibleSeconds;
            if (minimumVisibleSeconds <= 0f)
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

            while (Time.realtimeSinceStartup - startedAt < minimumVisibleSeconds)
            {
                await Task.Yield();
            }
        }

        private static async Task EnsureVisualHideSettledAsync(LoadingHudController controller, string signature)
        {
            if (controller == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalLoading] LoadingHudController obrigatorio ausente.");
            }

            int startFrame = Time.frameCount;
            await Task.Yield();
            while (Time.frameCount == startFrame)
            {
                await Task.Yield();
            }

            CanvasGroup rootGroup = controller.RootGroup;
            if (rootGroup == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] LoadingHudController rootGroup ausente after hide. contextSignature='{signature}'.");
            }

            if (rootGroup.alpha > 0f)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalLoading] LoadingHudController hide did not settle visually. alpha='{rootGroup.alpha:0.###}' contextSignature='{signature}'.");
            }
        }
    }
}


