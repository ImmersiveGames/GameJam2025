#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility
{
    public readonly struct IntroStageEntryEvent : IEvent
    {
        public IntroStageEntryEvent(IntroStageSession session, string source, SceneRouteKind routeKind)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            RouteKind = routeKind;
        }

        public IntroStageSession Session { get; }
        public string Source { get; }
        public SceneRouteKind RouteKind { get; }
    }

    public readonly struct IntroStageCompletedEvent : IEvent
    {
        public IntroStageCompletedEvent(IntroStageSession session, string source, bool wasSkipped, string reason)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            WasSkipped = wasSkipped;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public IntroStageSession Session { get; }
        public string Source { get; }
        public bool WasSkipped { get; }
        public string Reason { get; }
    }

    public readonly struct IntroStagePresentationContract
    {
        public IntroStagePresentationContract(
            PhaseDefinitionAsset phaseDefinitionRef,
            string sessionSignature,
            int selectionVersion,
            string localContentId,
            bool hasIntroStage)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            HasIntroStage = hasIntroStage;
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public string SessionSignature { get; }
        public int SelectionVersion { get; }
        public string LocalContentId { get; }
        public bool HasIntroStage { get; }

        public bool IsValid => PhaseDefinitionRef != null;
    }

    public interface IIntroStageSessionService
    {
        bool TryGetCurrentSession(out IntroStageSession session);
    }

    public interface IIntroStagePresenterRegistry
    {
        bool TryGetCurrentPresenter(out IIntroStagePresenter presenter);
        bool TryEnsureCurrentPresenter(IntroStageSession session, string source, out IIntroStagePresenter presenter);
    }

    public interface IIntroStagePresenterScopeResolver
    {
        bool TryResolvePresenters(IntroStageSession session, out IReadOnlyList<IIntroStagePresenter> presenters);
    }

    public interface IIntroStagePresenter
    {
        string PresenterSignature { get; }
        bool IsPresentationAttached { get; }
        bool CanServe(string sessionSignature);
        void AttachPresentation(IntroStagePresentationContract contract);
        void DetachPresentation(string reason);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageSessionService : IIntroStageSessionService, System.IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<SessionTransitionIntroStageActivationEvent> _introStageActivationBinding;
        private IntroStageSession _currentSession;
        private PhaseEntryIdentity _activePhaseEntryIdentity;
        private bool _disposed;

        public IntroStageSessionService()
        {
            _introStageActivationBinding = new EventBinding<SessionTransitionIntroStageActivationEvent>(OnIntroStageActivation);
            EventBus<SessionTransitionIntroStageActivationEvent>.Register(_introStageActivationBinding);

            DebugUtility.LogVerbose<IntroStageSessionService>(
                "[OBS][IntroStage] IntroStageSessionService registrado (SessionTransitionIntroStageActivationEvent -> IntroStage session bridge operacional).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentSession(out IntroStageSession session)
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
            EventBus<SessionTransitionIntroStageActivationEvent>.Unregister(_introStageActivationBinding);
        }

        private void OnIntroStageActivation(SessionTransitionIntroStageActivationEvent evt)
        {
            if (!evt.HasCanonicalPayload)
            {
                HardFailFastH1.Trigger(typeof(IntroStageSessionService),
                    "[FATAL][H1][IntroStage] SessionTransitionIntroStageActivationEvent invalido ao materializar IntroStageSession.");
            }

            IntroStageSession session = evt.Session;
            if (!session.IsValid || !session.PhaseEntryIdentity.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageSessionService),
                    "[FATAL][H1][IntroStage] SessionTransitionIntroStageActivationEvent sem identidade canonica valida.");
            }

            if (_activePhaseEntryIdentity.IsValid &&
                session.PhaseEntryIdentity != _activePhaseEntryIdentity &&
                session.PhaseLocalEntrySequence <= _currentSession.PhaseLocalEntrySequence)
            {
                DebugUtility.Log<IntroStageSessionService>(
                    $"[OBS][IntroStage] IntroStageActivationIgnored reason='stale_phase_entry_identity' expectedIdentity='{_activePhaseEntryIdentity}' receivedIdentity='{session.PhaseEntryIdentity}' expectedSessionSignature='{Normalize(_currentSession.SessionSignature)}' receivedSessionSignature='{Normalize(session.SessionSignature)}' source='{evt.Source}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            lock (_sync)
            {
                _currentSession = session;
                _activePhaseEntryIdentity = session.PhaseEntryIdentity;
            }

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageSessionUpdated rail='session_transition_activation' hasIntroStage='{session.HasIntroStage}' phaseEntryIdentity='{session.PhaseEntryIdentity}' v='{session.SelectionVersion}' entrySeq='{session.PhaseLocalEntrySequence}' sessionSignature='{session.SessionSignature}' phaseRuntimeSignature='{session.PhaseRuntimeSignature}' entrySignature='{session.EntrySignature}' reason='{session.Reason}' source='{evt.Source}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}

