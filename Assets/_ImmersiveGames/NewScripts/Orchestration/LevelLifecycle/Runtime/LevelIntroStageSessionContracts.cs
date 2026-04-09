#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct PhaseIntroStageEntryEvent : IEvent
    {
        public PhaseIntroStageEntryEvent(LevelIntroStageSession session, string source, _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime.SceneRouteKind routeKind)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            RouteKind = routeKind;
        }

        public LevelIntroStageSession Session { get; }
        public string Source { get; }
        public _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime.SceneRouteKind RouteKind { get; }
    }

    public readonly struct LevelIntroCompletedEvent : IEvent
    {
        public LevelIntroCompletedEvent(LevelIntroStageSession session, string source, bool wasSkipped, string reason)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            WasSkipped = wasSkipped;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public LevelIntroStageSession Session { get; }
        public string Source { get; }
        public bool WasSkipped { get; }
        public string Reason { get; }
    }

    public interface ILevelIntroStageSessionService
    {
        bool TryGetCurrentSession(out LevelIntroStageSession session);
    }

    public interface ILevelIntroStagePresenterRegistry
    {
        bool TryGetCurrentPresenter(out ILevelIntroStagePresenter presenter);
        bool TryEnsureCurrentPresenter(LevelIntroStageSession session, string source, out ILevelIntroStagePresenter presenter);
    }

    public interface ILevelIntroStagePresenterScopeResolver
    {
        bool TryResolvePresenters(LevelIntroStageSession session, out IReadOnlyList<ILevelIntroStagePresenter> presenters);
    }

    public interface ILevelIntroStagePresenter
    {
        string PresenterSignature { get; }
        bool IsPresentationAttached { get; }
        bool CanServe(string sessionSignature);
        void AttachPresentation(LevelStagePresentationContract contract);
        void DetachPresentation(string reason);
    }

    public sealed class LevelIntroStageSessionService : ILevelIntroStageSessionService, System.IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private LevelIntroStageSession _currentSession;
        private bool _disposed;

        public LevelIntroStageSessionService()
        {
            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseDefinitionSelected);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);

            DebugUtility.LogVerbose<LevelIntroStageSessionService>(
                "[OBS][IntroStage] LevelIntroStageSessionService registrado (PhaseDefinitionSelectedEvent -> IntroStage session bridge canonica).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentSession(out LevelIntroStageSession session)
        {
            lock (_sync)
            {
                session = _currentSession;
                return _currentSession.IsValid;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
        }

        private void OnPhaseDefinitionSelected(PhaseDefinitionSelectedEvent evt)
        {
            PhaseDefinitionAsset phaseDefinitionRef = evt.PhaseDefinitionRef;
            string phaseName = phaseDefinitionRef != null ? phaseDefinitionRef.name : "<none>";

            if (phaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageSessionService),
                    "[FATAL][H1][IntroStage] PhaseDefinitionSelectedEvent sem phaseDefinitionRef ao materializar intro session.");
                return;
            }

            string localContentId = phaseDefinitionRef.BuildCanonicalIntroContentId();

            LevelIntroStageDisposition disposition = phaseDefinitionRef.Intro != null && phaseDefinitionRef.Intro.hasIntroStage
                ? LevelIntroStageDisposition.HasIntro
                : LevelIntroStageDisposition.NoIntro;

            LevelIntroStageSession session = new LevelIntroStageSession(
                phaseDefinitionRef,
                localContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.SelectionSignature,
                phaseDefinitionRef.Intro != null ? phaseDefinitionRef.Intro.introPresenterPrefab : null,
                disposition);

            lock (_sync)
            {
                _currentSession = session;
            }

            DebugUtility.Log<LevelIntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageSessionUpdated rail='phase' contentName='{phaseName}' routeId='{evt.MacroRouteId}' disposition='{session.Disposition}' v='{session.SelectionVersion}' signature='{session.LevelSignature}' reason='{session.Reason}'.",
                DebugUtility.Colors.Info);
        }

    }
}
