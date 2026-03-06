using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class LevelStageOrchestrator : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly EventBinding<LevelSwapLocalAppliedEvent> _levelSwapLocalAppliedBinding;

        private int _lastProcessedSelectionVersion = -1;
        private string _lastProcessedLevelSignature = string.Empty;

        public LevelStageOrchestrator()
        {
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            _levelSwapLocalAppliedBinding = new EventBinding<LevelSwapLocalAppliedEvent>(OnLevelSwapLocalApplied);

            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Register(_levelSwapLocalAppliedBinding);
        }

        public void Dispose()
        {
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
            EventBus<LevelSwapLocalAppliedEvent>.Unregister(_levelSwapLocalAppliedBinding);
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (evt.Context.TransitionProfileId != SceneFlowProfileId.Gameplay)
            {
                return;
            }

            if (!TryResolveIntroStageDependencies(out var gameLoopService, out var coordinator))
            {
                return;
            }

            if (!IsGameplayScene())
            {
                return;
            }

            if (!TryResolveRestartContext(out var restartContextService)
                || !restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
                || !snapshot.IsValid
                || !snapshot.HasLevelRef)
            {
                DebugUtility.LogVerbose<LevelStageOrchestrator>(
                    "[LevelFlow] IntroStage via SceneFlowCompleted ignored: canonical snapshot unavailable/invalid.",
                    DebugUtility.Colors.Info);
                return;
            }

            string levelSig = snapshot.LevelSignature;
            if (!string.IsNullOrWhiteSpace(levelSig))
            {
                if (string.Equals(levelSig, _lastProcessedLevelSignature, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<LevelStageOrchestrator>(
                        $"[LevelFlow] IntroStage via SceneFlowCompleted skipped reason='dedupe_level_signature' levelSignature='{levelSig}' routeId='{snapshot.RouteId}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _lastProcessedLevelSignature = levelSig;
                _lastProcessedSelectionVersion = snapshot.SelectionVersion;
            }
            else
            {
                if (snapshot.SelectionVersion < _lastProcessedSelectionVersion)
                {
                    int previousVersion = _lastProcessedSelectionVersion;
                    int nextVersion = snapshot.SelectionVersion;
                    _lastProcessedSelectionVersion = -1;
                    _lastProcessedLevelSignature = string.Empty;

                    DebugUtility.Log<LevelStageOrchestrator>(
                        $"[OBS][LevelFlow] LevelStageDedupeReset reason='selection_version_rewind' prev='{previousVersion}' next='{nextVersion}' routeId='{snapshot.RouteId}'.",
                        DebugUtility.Colors.Info);
                }

                if (snapshot.SelectionVersion <= _lastProcessedSelectionVersion)
                {
                    return;
                }

                _lastProcessedSelectionVersion = snapshot.SelectionVersion;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelSignature = string.IsNullOrWhiteSpace(levelSig)
                ? $"level:{snapshot.LevelRef.name}|route:{snapshot.RouteId}|reason:SceneFlow/Completed"
                : levelSig;

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='SceneFlowCompleted' levelRef='{snapshot.LevelRef.name}' v='{snapshot.SelectionVersion}' reason='SceneFlow/Completed' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            gameLoopService.RequestIntroStageStart();
            var context = new IntroStageContext(
                contextSignature: levelSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeSceneName,
                reason: "SceneFlow/Completed");

            _ = coordinator.RunIntroStageAsync(context);
        }

        private void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt)
        {
            if (evt.SelectionVersion <= _lastProcessedSelectionVersion)
            {
                return;
            }

            if (!TryResolveIntroStageDependencies(out var gameLoopService, out var coordinator))
            {
                return;
            }

            if (!IsGameplayScene())
            {
                return;
            }

            _lastProcessedSelectionVersion = evt.SelectionVersion;
            _lastProcessedLevelSignature = string.IsNullOrWhiteSpace(evt.LevelSignature)
                ? string.Empty
                : evt.LevelSignature;

            string normalizedReason = string.IsNullOrWhiteSpace(evt.Reason)
                ? "LevelFlow/SwapLevelLocal"
                : evt.Reason;
            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelSignature = string.IsNullOrWhiteSpace(evt.LevelSignature)
                ? $"level:{(evt.LevelRef != null ? evt.LevelRef.name : "<none>")}|route:{evt.MacroRouteId}|reason:{normalizedReason}"
                : evt.LevelSignature;

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='LevelSwapLocal' levelRef='{(evt.LevelRef != null ? evt.LevelRef.name : "<none>")}' v='{evt.SelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            gameLoopService.RequestIntroStageStart();
            var context = new IntroStageContext(
                contextSignature: levelSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeSceneName,
                reason: normalizedReason);

            _ = coordinator.RunIntroStageAsync(context);
        }

        private static bool TryResolveIntroStageDependencies(out IGameLoopService gameLoopService, out IIntroStageCoordinator coordinator)
        {
            gameLoopService = null;
            coordinator = null;

            if (!DependencyManager.HasInstance)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out gameLoopService) || gameLoopService == null)
            {
                DebugUtility.LogWarning<LevelStageOrchestrator>(
                    "[LevelFlow] IGameLoopService unavailable; IntroStage will not start.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out coordinator) || coordinator == null)
            {
                DebugUtility.LogWarning<LevelStageOrchestrator>(
                    "[LevelFlow] IIntroStageCoordinator unavailable; IntroStage will not start.");
                return false;
            }

            return true;
        }

        private static bool TryResolveRestartContext(out IRestartContextService restartContextService)
        {
            restartContextService = null;

            if (!DependencyManager.HasInstance)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out restartContextService)
                   && restartContextService != null;
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.HasInstance
                && DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier)
                && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return string.Equals(SceneManager.GetActiveScene().name, "GameplayScene", StringComparison.Ordinal);
        }
    }
}
