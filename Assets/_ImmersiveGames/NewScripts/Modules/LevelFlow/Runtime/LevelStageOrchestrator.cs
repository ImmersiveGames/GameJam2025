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
    /// <summary>
    /// Orquestrador de estágios de level no domínio LevelFlow.
    /// Centraliza o gatilho da IntroStage para evitar ownership no bridge de InputMode.
    /// </summary>
    public sealed class LevelStageOrchestrator : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly EventBinding<LevelSwapLocalAppliedEvent> _levelSwapLocalAppliedBinding;

        private int _lastProcessedSelectionVersion = -1;

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
                || !snapshot.HasLevelId
                || !snapshot.HasContentId)
            {
                DebugUtility.LogVerbose<LevelStageOrchestrator>(
                    "[LevelFlow] IntroStage via SceneFlowCompleted ignorada: snapshot canônico indisponível/inválido.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (snapshot.SelectionVersion <= _lastProcessedSelectionVersion)
            {
                return;
            }

            _lastProcessedSelectionVersion = snapshot.SelectionVersion;

            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelSignature = string.IsNullOrWhiteSpace(snapshot.LevelSignature)
                ? LevelContextSignature.Create(snapshot.LevelId, snapshot.RouteId, "SceneFlow/Completed", snapshot.ContentId).Value
                : snapshot.LevelSignature;

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='SceneFlowCompleted' levelId='{snapshot.LevelId}' contentId='{snapshot.ContentId}' v='{snapshot.SelectionVersion}' reason='SceneFlow/Completed' levelSignature='{levelSignature}'.",
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

            string normalizedReason = string.IsNullOrWhiteSpace(evt.Reason)
                ? "LevelFlow/SwapLevelLocal"
                : evt.Reason;
            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelSignature = string.IsNullOrWhiteSpace(evt.LevelSignature)
                ? LevelContextSignature.Create(evt.LevelId, evt.RouteId, normalizedReason, evt.ContentId).Value
                : evt.LevelSignature;

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='LevelSwapLocal' levelId='{evt.LevelId}' contentId='{evt.ContentId}' v='{evt.SelectionVersion}' reason='{normalizedReason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            gameLoopService.RequestIntroStageStart();
            var context = new IntroStageContext(
                contextSignature: levelSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeSceneName,
                reason: normalizedReason);

            _ = coordinator.RunIntroStageAsync(context);
        }

        private static bool TryResolveIntroStageDependencies(
            out IGameLoopService gameLoopService,
            out IIntroStageCoordinator coordinator)
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
                    "[LevelFlow] IGameLoopService indisponível; IntroStage não será iniciada.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out coordinator) || coordinator == null)
            {
                DebugUtility.LogWarning<LevelStageOrchestrator>(
                    "[LevelFlow] IIntroStageCoordinator indisponível; IntroStage não será iniciada.");
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
