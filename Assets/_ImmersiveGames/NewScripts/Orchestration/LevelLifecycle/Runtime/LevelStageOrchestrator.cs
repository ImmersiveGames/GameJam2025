using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public class LevelLifecycleStageOrchestrator : IDisposable
    {
        private readonly EventBinding<LevelEnteredEvent> _levelEnteredBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private readonly object _sync = new();
        private int _lastProcessedSelectionVersion = -1;
        private LevelIntroStageSession _pendingGameplaySession;
        private string _pendingGameplaySource = string.Empty;
        private string _pendingGameplayReason = string.Empty;
        private bool _hasPendingGameplayIntro;

        public LevelLifecycleStageOrchestrator()
        {
            _levelEnteredBinding = new EventBinding<LevelEnteredEvent>(OnLevelEntered);
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            EventBus<LevelEnteredEvent>.Register(_levelEnteredBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        public void Dispose()
        {
            EventBus<LevelEnteredEvent>.Unregister(_levelEnteredBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }

        private void OnLevelEntered(LevelEnteredEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleStageOrchestrator),
                    "[FATAL][H1][LevelLifecycle] Invalid LevelEnteredEvent received.");
            }

            if (!TryAdvanceDedupe(evt.Session.SelectionVersion, evt.Session.LevelSignature, evt.Source))
            {
                return;
            }

            if (ShouldDeferGameplayIntro(evt))
            {
                QueuePendingGameplayIntro(evt);
                return;
            }

            DispatchIntroStage(
                evt.Source,
                evt.Session,
                evt.RouteKind,
                Normalize(evt.Session.Reason),
                isLocalSwap: string.Equals(evt.Source, "LevelSwapLocal", StringComparison.Ordinal));
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (evt.context.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            LevelIntroStageSession pendingSession;
            string pendingSource;
            string pendingReason;

            lock (_sync)
            {
                if (!_hasPendingGameplayIntro)
                {
                    return;
                }

                pendingSession = _pendingGameplaySession;
                pendingSource = _pendingGameplaySource;
                pendingReason = _pendingGameplayReason;
                _hasPendingGameplayIntro = false;
                _pendingGameplaySession = default;
                _pendingGameplaySource = string.Empty;
                _pendingGameplayReason = string.Empty;
            }

            DebugUtility.Log<LevelLifecycleStageOrchestrator>(
                $"[OBS][LevelLifecycle] EnterStageReleasedOnSceneTransitionCompleted source='{pendingSource}' levelRef='{pendingSession.LevelRef.name}' v='{pendingSession.SelectionVersion}' reason='{Normalize(pendingReason)}' levelSignature='{Normalize(pendingSession.LevelSignature)}' routeKind='{evt.context.RouteKind}' sceneTransitionSignature='{SceneTransitionSignature.Compute(evt.context)}'.",
                DebugUtility.Colors.Info);

            DispatchIntroStage(
                pendingSource,
                pendingSession,
                evt.context.RouteKind,
                pendingReason,
                isLocalSwap: string.Equals(pendingSource, "LevelSwapLocal", StringComparison.Ordinal));
        }

        private bool TryAdvanceDedupe(int selectionVersion, string levelSignature, string source)
        {
            if (selectionVersion < _lastProcessedSelectionVersion)
            {
                _lastProcessedSelectionVersion = -1;
            }

            if (selectionVersion <= _lastProcessedSelectionVersion)
            {
                DebugUtility.LogVerbose<LevelLifecycleStageOrchestrator>(
                    $"[LevelLifecycle] EnterStage skipped reason='dedupe_selection_version' selectionVersion='{selectionVersion}' source='{source}' levelSignature='{Normalize(levelSignature)}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            _lastProcessedSelectionVersion = selectionVersion;
            return true;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static bool ShouldDeferGameplayIntro(LevelEnteredEvent evt)
        {
            return evt.RouteKind == SceneRouteKind.Gameplay
                   && string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal);
        }

        private void QueuePendingGameplayIntro(LevelEnteredEvent evt)
        {
            lock (_sync)
            {
                _pendingGameplaySession = evt.Session;
                _pendingGameplaySource = evt.Source;
                _pendingGameplayReason = Normalize(evt.Session.Reason);
                _hasPendingGameplayIntro = true;
            }

            DebugUtility.Log<LevelLifecycleStageOrchestrator>(
                $"[OBS][LevelLifecycle] EnterStageDeferred source='{evt.Source}' levelRef='{evt.Session.LevelRef.name}' v='{evt.Session.SelectionVersion}' disposition='{evt.Session.Disposition}' reason='{Normalize(evt.Session.Reason)}' levelSignature='{Normalize(evt.Session.LevelSignature)}' gate='SceneTransitionCompletedEvent'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolvePresenterRegistry(out ILevelIntroStagePresenterRegistry presenterRegistry)
        {
            presenterRegistry = null;

            if (!DependencyManager.Provider.TryGetGlobal(out presenterRegistry) ||
                presenterRegistry == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleStageOrchestrator),
                    "[FATAL][H1][LevelLifecycle] Missing ILevelIntroStagePresenterRegistry for level entry.");
                return false;
            }

            return true;
        }

        private void DispatchIntroStage(
            string source,
            _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session,
            SceneRouteKind routeKind,
            string reason,
            bool isLocalSwap)
        {
            if (isLocalSwap)
            {
                DebugUtility.LogVerbose<LevelLifecycleStageOrchestrator>(
                    $"[OBS][LevelLifecycle] EnterStageLocalSwapDispatch source='{source}' levelRef='{session.LevelRef.name}' v='{session.SelectionVersion}' reason='{reason}' levelSignature='{session.LevelSignature}'.",
                    DebugUtility.Colors.Info);
            }

            if (!session.HasIntroStage)
            {
                PublishLevelIntroCompleted(session, source, wasSkipped: true, reason: "LevelIntro/NoIntro");
                return;
            }

            if (!TryResolvePresenterRegistry(out var presenterRegistry))
            {
                return;
            }

            if (!presenterRegistry.TryEnsureCurrentPresenter(session, source, out _))
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleStageOrchestrator),
                    $"[FATAL][H1][LevelLifecycle] Intro session requires a canonical level presenter but none is registered. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}'.");
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            string levelName = session.LevelRef != null ? session.LevelRef.name : "<none>";

            DebugUtility.Log<LevelLifecycleStageOrchestrator>(
                $"[OBS][LevelLifecycle] EnterStageStartRequested source='{source}' levelRef='{levelName}' v='{session.SelectionVersion}' disposition='{session.Disposition}' reason='{reason}' levelSignature='{session.LevelSignature}'.",
                DebugUtility.Colors.Info);

            var context = new IntroStageContext(
                session: session,
                routeKind: routeKind,
                targetScene: activeSceneName,
                reason: reason);

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator) || coordinator == null)
            {
                HardFailFastH1.Trigger(typeof(LevelLifecycleStageOrchestrator),
                    $"[FATAL][H1][LevelLifecycle] Missing IIntroStageCoordinator for EnterStage dispatch. source='{source}' levelSignature='{session.LevelSignature}'.");
            }

            if (coordinator != null)
            {
                _ = coordinator.RunIntroStageAsync(context);
            }
        }

        private static void PublishLevelIntroCompleted(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session, string source, bool wasSkipped, string reason)
        {
            string levelName = session.LevelRef != null ? session.LevelRef.name : "<none>";
            DebugUtility.Log<LevelLifecycleStageOrchestrator>(
                $"[OBS][LevelLifecycle] EnterStageCompletedPublished source='{source}' levelRef='{levelName}' v='{session.SelectionVersion}' signature='{session.LevelSignature}' skipped='{wasSkipped.ToString().ToLowerInvariant()}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelIntroCompletedEvent>.Raise(new LevelIntroCompletedEvent(session, source, wasSkipped, reason));
        }
    }
}
