using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class LevelStageOrchestrator : IDisposable
    {
        private readonly EventBinding<LevelEnteredEvent> _levelEnteredBinding;
        private readonly EventBinding<GameLoopStateEnteredEvent> _gameLoopStateEnteredBinding;

        private int _lastProcessedSelectionVersion = -1;
        private LevelIntroStageSession _pendingSession;
        private string _pendingSource = string.Empty;
        private bool _hasPendingIntro;

        public LevelStageOrchestrator()
        {
            _levelEnteredBinding = new EventBinding<LevelEnteredEvent>(OnLevelEntered);
            _gameLoopStateEnteredBinding = new EventBinding<GameLoopStateEnteredEvent>(OnGameLoopStateEntered);
            EventBus<LevelEnteredEvent>.Register(_levelEnteredBinding);
            EventBus<GameLoopStateEnteredEvent>.Register(_gameLoopStateEnteredBinding);
        }

        public void Dispose()
        {
            EventBus<LevelEnteredEvent>.Unregister(_levelEnteredBinding);
            EventBus<GameLoopStateEnteredEvent>.Unregister(_gameLoopStateEnteredBinding);
        }

        private void OnLevelEntered(LevelEnteredEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    "[FATAL][H1][LevelFlow] Invalid LevelEnteredEvent received.");
            }

            if (!TryAdvanceDedupe(evt.Session.SelectionVersion, evt.Session.LevelSignature, evt.Source))
            {
                return;
            }

            if (!TryResolvePresenterRegistry(out var presenterRegistry))
            {
                return;
            }

            bool presenterReady = presenterRegistry.TryEnsureCurrentPresenter(evt.Session, evt.Source);
            if (evt.Session.HasIntroStage && !presenterReady)
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    $"[FATAL][H1][LevelFlow] Intro session requires a canonical level presenter but none is registered. source='{evt.Source}' levelRef='{evt.Session.LevelRef.name}' signature='{evt.Session.LevelSignature}'.");
            }

            if (!evt.Session.HasIntroStage)
            {
                ClearPendingIntro();
                return;
            }

            _pendingSession = evt.Session;
            _pendingSource = evt.Source;
            _hasPendingIntro = true;

            if (IsPlaying())
            {
                RequestIntroRearm();
            }

            if (IsReady())
            {
                TryDispatchPendingIntro("level_entered_ready");
            }
        }

        private void OnGameLoopStateEntered(GameLoopStateEnteredEvent evt)
        {
            if (!_hasPendingIntro || evt.StateId != GameLoopStateId.Ready)
            {
                return;
            }

            TryDispatchPendingIntro(evt.IsActive ? "game_loop_ready_active" : "game_loop_ready");
        }

        private bool TryAdvanceDedupe(int selectionVersion, string levelSignature, string source)
        {
            if (selectionVersion < _lastProcessedSelectionVersion)
            {
                _lastProcessedSelectionVersion = -1;
            }

            if (selectionVersion <= _lastProcessedSelectionVersion)
            {
                DebugUtility.LogVerbose<LevelStageOrchestrator>(
                    $"[LevelFlow] IntroStage skipped reason='dedupe_selection_version' selectionVersion='{selectionVersion}' source='{source}' levelSignature='{Normalize(levelSignature)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _lastProcessedSelectionVersion = selectionVersion;
            return true;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static bool TryResolvePresenterRegistry(out ILevelIntroStagePresenterRegistry presenterRegistry)
        {
            presenterRegistry = null;

            if (!DependencyManager.Provider.TryGetGlobal<ILevelIntroStagePresenterRegistry>(out presenterRegistry) ||
                presenterRegistry == null)
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    "[FATAL][H1][LevelFlow] Missing ILevelIntroStagePresenterRegistry for level entry.");
                return false;
            }

            return true;
        }

        private bool IsPlaying()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) &&
                   gameLoopService != null &&
                   string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
        }

        private bool IsReady()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) &&
                   gameLoopService != null &&
                   string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Ready), StringComparison.Ordinal);
        }

        private void RequestIntroRearm()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    $"[FATAL][H1][LevelFlow] Missing IGameLoopService for intro rearm. levelSignature='{_pendingSession.LevelSignature}'.");
            }

            gameLoopService.RequestRearm();
            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageRearmRequested source='{_pendingSource}' levelRef='{_pendingSession.LevelRef.name}' v='{_pendingSession.SelectionVersion}' reason='{Normalize(_pendingSession.Reason)}' levelSignature='{_pendingSession.LevelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private void TryDispatchPendingIntro(string trigger)
        {
            _ = trigger;

            if (!_hasPendingIntro)
            {
                return;
            }

            LevelIntroStageSession session = _pendingSession;
            string source = _pendingSource;
            ClearPendingIntro();
            DispatchIntroStage(source, session, Normalize(session.Reason), isLocalSwap: string.Equals(source, "LevelSwapLocal", StringComparison.Ordinal));
        }

        private void ClearPendingIntro()
        {
            _pendingSession = default;
            _pendingSource = string.Empty;
            _hasPendingIntro = false;
        }

        private void DispatchIntroStage(
            string source,
            LevelIntroStageSession session,
            string reason,
            bool isLocalSwap)
        {
            if (isLocalSwap)
            {
                DebugUtility.LogVerbose<LevelStageOrchestrator>(
                    $"[OBS][IntroStageController] IntroStageLocalSwapDispatch source='{source}' levelRef='{session.LevelRef.name}' v='{session.SelectionVersion}' reason='{reason}' levelSignature='{session.LevelSignature}'.",
                    DebugUtility.Colors.Info);
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelName = session.LevelRef != null ? session.LevelRef.name : "<none>";

            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] IntroStageStartRequested source='{source}' levelRef='{levelName}' v='{session.SelectionVersion}' disposition='{session.Disposition}' reason='{reason}' levelSignature='{session.LevelSignature}'.",
                DebugUtility.Colors.Info);

            var context = new IntroStageContext(
                session: session,
                targetScene: activeSceneName,
                reason: reason);

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator) || coordinator == null)
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    $"[FATAL][H1][LevelFlow] Missing IIntroStageCoordinator for intro dispatch. source='{source}' levelSignature='{session.LevelSignature}'.");
            }

            _ = coordinator.RunIntroStageAsync(context);
        }
    }
}
