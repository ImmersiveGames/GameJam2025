using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class LevelStageOrchestrator : IDisposable
    {
        private readonly EventBinding<LevelEnteredEvent> _levelEnteredBinding;

        private int _lastProcessedSelectionVersion = -1;

        public LevelStageOrchestrator()
        {
            _levelEnteredBinding = new EventBinding<LevelEnteredEvent>(OnLevelEntered);
            EventBus<LevelEnteredEvent>.Register(_levelEnteredBinding);
        }

        public void Dispose()
        {
            EventBus<LevelEnteredEvent>.Unregister(_levelEnteredBinding);
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

            if (!evt.Session.HasIntroStage)
            {
                PublishLevelIntroCompleted(evt.Session, evt.Source, wasSkipped: true, reason: "LevelIntro/NoIntro");
                return;
            }

            if (!TryResolvePresenterRegistry(out var presenterRegistry))
            {
                return;
            }

            if (!presenterRegistry.TryEnsureCurrentPresenter(evt.Session, evt.Source, out var presenter) ||
                presenter == null ||
                !presenter.IsReady ||
                !string.Equals(presenter.PresenterSignature, evt.Session.LevelSignature, StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(LevelStageOrchestrator),
                    $"[FATAL][H1][LevelFlow] Intro session requires a canonical level presenter but none is registered. source='{evt.Source}' levelRef='{evt.Session.LevelRef.name}' signature='{evt.Session.LevelSignature}'.");
            }

            DispatchIntroStage(evt.Source, evt.Session, Normalize(evt.Session.Reason), isLocalSwap: string.Equals(evt.Source, "LevelSwapLocal", StringComparison.Ordinal));
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

        private static void PublishLevelIntroCompleted(LevelIntroStageSession session, string source, bool wasSkipped, string reason)
        {
            string levelName = session.LevelRef != null ? session.LevelRef.name : "<none>";
            DebugUtility.Log<LevelStageOrchestrator>(
                $"[OBS][IntroStageController] LevelIntroCompletedPublished source='{source}' levelRef='{levelName}' v='{session.SelectionVersion}' signature='{session.LevelSignature}' skipped='{wasSkipped.ToString().ToLowerInvariant()}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            EventBus<LevelIntroCompletedEvent>.Raise(new LevelIntroCompletedEvent(session, source, wasSkipped, reason));
        }
    }
}
