using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;
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
            DefaultLevelIntroOverlayPresenter.EnsureInstalled();

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
            if (evt.context.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            if (!TryResolveIntroStageDependencies(out var gameLoopService, out var coordinator))
            {
                return;
            }

            if (!TryResolveRestartContext(out var restartContextService) ||
                !restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef)
            {
                DebugUtility.LogVerbose<LevelStageOrchestrator>(
                    "[LevelFlow] IntroStage via SceneFlowCompleted ignored: canonical snapshot unavailable/invalid.",
                    DebugUtility.Colors.Info);
                return;
            }

            string levelSig = snapshot.LevelSignature;
            if (!AdvanceDedupe(snapshot.SelectionVersion, levelSig, snapshot.MacroRouteId.ToString()))
            {
                return;
            }

            bool hasIntroStage = ResolveHasIntroStage();
            TryDispatchIntroStage(
                source: "SceneFlowCompleted",
                routeKind: evt.context.RouteKind,
                routeId: snapshot.MacroRouteId,
                levelRef: snapshot.LevelRef,
                selectionVersion: snapshot.SelectionVersion,
                reason: "SceneFlow/Completed",
                existingLevelSignature: levelSig,
                hasIntroStage: hasIntroStage,
                gameLoopService: gameLoopService,
                coordinator: coordinator);
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

            _lastProcessedSelectionVersion = evt.SelectionVersion;
            _lastProcessedLevelSignature = string.IsNullOrWhiteSpace(evt.LevelSignature)
                ? string.Empty
                : evt.LevelSignature;

            string normalizedReason = string.IsNullOrWhiteSpace(evt.Reason)
                ? "LevelFlow/SwapLevelLocal"
                : evt.Reason;
            bool hasIntroStage = ResolveHasIntroStage();
            TryDispatchIntroStage(
                source: "LevelSwapLocal",
                routeKind: SceneRouteKind.Gameplay,
                routeId: evt.MacroRouteId,
                levelRef: evt.LevelRef,
                selectionVersion: evt.SelectionVersion,
                reason: normalizedReason,
                existingLevelSignature: evt.LevelSignature,
                hasIntroStage: hasIntroStage,
                gameLoopService: gameLoopService,
                coordinator: coordinator);
        }

        private bool AdvanceDedupe(int selectionVersion, string levelSig, string routeId)
        {
            if (!string.IsNullOrWhiteSpace(levelSig))
            {
                if (string.Equals(levelSig, _lastProcessedLevelSignature, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<LevelStageOrchestrator>(
                        $"[LevelFlow] IntroStage via SceneFlowCompleted skipped reason='dedupe_level_signature' levelSignature='{levelSig}' routeId='{routeId}'.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                _lastProcessedLevelSignature = levelSig;
                _lastProcessedSelectionVersion = selectionVersion;
                return true;
            }

            if (selectionVersion < _lastProcessedSelectionVersion)
            {
                int previousVersion = _lastProcessedSelectionVersion;
                int nextVersion = selectionVersion;
                _lastProcessedSelectionVersion = -1;
                _lastProcessedLevelSignature = string.Empty;

                DebugUtility.Log<LevelStageOrchestrator>(
                    $"[OBS][LevelFlow] LevelStageDedupeReset reason='selection_version_rewind' prev='{previousVersion}' next='{nextVersion}' routeId='{routeId}'.",
                    DebugUtility.Colors.Info);
            }

            if (selectionVersion <= _lastProcessedSelectionVersion)
            {
                return false;
            }

            _lastProcessedSelectionVersion = selectionVersion;
            return true;
        }

        private static bool TryResolveIntroStageDependencies(out IGameLoopService gameLoopService, out IIntroStageCoordinator coordinator)
        {
            gameLoopService = null;
            coordinator = null;

            if (!DependencyManager.HasInstance)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out gameLoopService) || gameLoopService == null)
            {
                DebugUtility.LogWarning<LevelStageOrchestrator>(
                    "[LevelFlow] IGameLoopService unavailable; IntroStage will not start.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out coordinator) || coordinator == null)
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

            return DependencyManager.Provider.TryGetGlobal(out restartContextService)
                   && restartContextService != null;
        }

        private static bool ResolveHasIntroStage()
        {
            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<ILevelStagePresentationService>(out var presentationService) &&
                presentationService != null &&
                presentationService.TryGetCurrentContract(out LevelStagePresentationContract contract))
            {
                return contract.HasIntroStage;
            }

            return true;
        }

        private static string BuildLevelSignature(LevelDefinitionAsset levelRef, SceneRouteId routeId, string reason, string existingSignature)
        {
            if (!string.IsNullOrWhiteSpace(existingSignature))
            {
                return existingSignature.Trim();
            }

            string levelName = levelRef != null ? levelRef.name : "<none>";
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/Unspecified" : reason.Trim();
            return $"level:{levelName}|route:{routeId}|reason:{normalizedReason}";
        }

        private void TryDispatchIntroStage(
            string source,
            SceneRouteKind routeKind,
            SceneRouteId routeId,
            LevelDefinitionAsset levelRef,
            int selectionVersion,
            string reason,
            string existingLevelSignature,
            bool hasIntroStage,
            IGameLoopService gameLoopService,
            IIntroStageCoordinator coordinator)
        {
            string levelSignature = BuildLevelSignature(levelRef, routeId, reason, existingLevelSignature);
            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelName = levelRef != null ? levelRef.name : "<none>";

            if (!hasIntroStage)
            {
                DebugUtility.Log<LevelStageOrchestrator>(
                    $"[OBS][IntroStageController] IntroStageSkipped source='{source}' levelRef='{levelName}' v='{selectionVersion}' reason='level_has_no_intro' levelSignature='{levelSignature}'.",
                    DebugUtility.Colors.Info);
                gameLoopService.RequestStart();
                return;
            }

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='{source}' levelRef='{levelName}' v='{selectionVersion}' reason='{reason}' levelSignature='{levelSignature}'.",
                DebugUtility.Colors.Info);

            gameLoopService.RequestIntroStageStart();
            var context = new IntroStageContext(
                contextSignature: levelSignature,
                routeKind: routeKind,
                targetScene: activeSceneName,
                reason: reason);

            _ = coordinator.RunIntroStageAsync(context);
        }
    }

    [DisallowMultipleComponent]
    public sealed class DefaultLevelIntroOverlayPresenter : MonoBehaviour
    {
        private const string RootName = "__DefaultLevelIntroOverlayPresenter";
        private static bool _installed;

        private readonly Rect _panelRect = new(16f, 16f, 380f, 180f);

        private IGameLoopService _gameLoopService;
        private IIntroStageControlService _introStageControlService;
        private ILevelStagePresentationService _levelStagePresentationService;

        public static void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (existing.GetComponent<DefaultLevelIntroOverlayPresenter>() == null)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][LevelFlow] Root '{RootName}' existe sem DefaultLevelIntroOverlayPresenter.");
                }

                _installed = true;
                return;
            }

            var root = new GameObject(RootName);
            DontDestroyOnLoad(root);
            root.AddComponent<DefaultLevelIntroOverlayPresenter>();
            _installed = true;
        }

        private void OnGUI()
        {
            if (!TryBuildViewModel(out var model))
            {
                return;
            }

            GUILayout.BeginArea(_panelRect, GUI.skin.box);
            GUILayout.Label("Level Intro Active");
            GUILayout.Label($"Level: {model.LevelName}");
            GUILayout.Label($"State: {model.StateName}");
            GUILayout.Label($"Signature: {model.LevelSignature}");

            if (GUILayout.Button("Start Gameplay"))
            {
                model.ControlService.CompleteIntroStage("LevelIntro/ConfirmButton");
            }

            GUILayout.EndArea();
        }

        private bool TryBuildViewModel(out IntroOverlayViewModel model)
        {
            model = default;
            ResolveDependencies();

            if (_gameLoopService == null ||
                _introStageControlService == null ||
                _levelStagePresentationService == null)
            {
                return false;
            }

            if (!_levelStagePresentationService.TryGetCurrentContract(out LevelStagePresentationContract contract) ||
                !contract.IsValid ||
                !contract.HasIntroStage)
            {
                return false;
            }

            bool stateIsIntro = string.Equals(
                _gameLoopService.CurrentStateIdName,
                nameof(GameLoopStateId.IntroStage),
                StringComparison.Ordinal);

            if (!stateIsIntro || !_introStageControlService.IsIntroStageActive)
            {
                return false;
            }

            model = new IntroOverlayViewModel(
                _introStageControlService,
                _gameLoopService.CurrentStateIdName,
                contract.LevelRef != null ? contract.LevelRef.name : "<none>",
                string.IsNullOrWhiteSpace(contract.LevelSignature) ? "<none>" : contract.LevelSignature.Trim());
            return true;
        }

        private void ResolveDependencies()
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (_gameLoopService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
            }

            if (_introStageControlService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _introStageControlService);
            }

            if (_levelStagePresentationService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _levelStagePresentationService);
            }
        }

        private readonly struct IntroOverlayViewModel
        {
            public IntroOverlayViewModel(
                IIntroStageControlService controlService,
                string stateName,
                string levelName,
                string levelSignature)
            {
                ControlService = controlService;
                StateName = string.IsNullOrWhiteSpace(stateName) ? "<none>" : stateName.Trim();
                LevelName = string.IsNullOrWhiteSpace(levelName) ? "<none>" : levelName.Trim();
                LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? "<none>" : levelSignature.Trim();
            }

            public IIntroStageControlService ControlService { get; }
            public string StateName { get; }
            public string LevelName { get; }
            public string LevelSignature { get; }
        }
    }
}
