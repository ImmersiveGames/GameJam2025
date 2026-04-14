#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
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
            bool hasIntroStage,
            bool hasRunResultStage)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            HasIntroStage = hasIntroStage;
            HasRunResultStage = hasRunResultStage;
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public string SessionSignature { get; }
        public int SelectionVersion { get; }
        public string LocalContentId { get; }
        public bool HasIntroStage { get; }
        public bool HasRunResultStage { get; }

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
        private readonly EventBinding<GameplayPhaseRuntimeMaterializedEvent> _phaseRuntimeMaterializedBinding;
        private IntroStageSession _currentSession;
        private bool _disposed;

        public IntroStageSessionService()
        {
            _phaseRuntimeMaterializedBinding = new EventBinding<GameplayPhaseRuntimeMaterializedEvent>(OnGameplayPhaseRuntimeMaterialized);
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Register(_phaseRuntimeMaterializedBinding);

            DebugUtility.LogVerbose<IntroStageSessionService>(
                "[OBS][IntroStage] IntroStageSessionService registrado (GameplayPhaseRuntimeMaterializedEvent -> IntroStage session bridge operacional).",
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
            EventBus<GameplayPhaseRuntimeMaterializedEvent>.Unregister(_phaseRuntimeMaterializedBinding);
        }

        private void OnGameplayPhaseRuntimeMaterialized(GameplayPhaseRuntimeMaterializedEvent evt)
        {
            GameplayPhaseRuntimeSnapshot runtime = evt.Runtime;
            PhaseDefinitionAsset phaseDefinitionRef = runtime.PhaseDefinitionRef;
            string phaseName = phaseDefinitionRef != null ? phaseDefinitionRef.name : "<none>";

            if (phaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageSessionService),
                    "[FATAL][H1][IntroStage] GameplayPhaseRuntimeMaterializedEvent sem phaseDefinitionRef ao materializar intro session.");
                return;
            }

            string localContentId = phaseDefinitionRef.BuildCanonicalIntroContentId();
            bool hasIntroStage = runtime.HasIntroStage;
            bool hasRunResultStage = runtime.HasRunResultStage;

            if (!hasIntroStage)
            {
                DebugUtility.Log<IntroStageSessionService>(
                    $"[OBS][IntroStage] IntroStageContractMaterialized contentName='{phaseName}' source='GameplayPhaseRuntime' phaseSignature='{runtime.PhaseRuntimeSignature}' hasIntroStage='false'.",
                    DebugUtility.Colors.Info);
            }

            IntroStageSession session = runtime.CreateIntroStageSession(
                localContentId,
                runtime.SessionContext.Reason,
                runtime.SessionContext.SelectionVersion,
                evt.PhaseLocalEntrySequence,
                runtime.PhaseRuntimeSignature,
                evt.EntrySignature);

            lock (_sync)
            {
                _currentSession = session;
            }

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageContractMaterialized contentName='{phaseName}' hasIntroStage='{session.HasIntroStage}' hasRunResultStage='{session.HasRunResultStage}' source='GameplayPhaseRuntime'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][RunResultStage] RunResultStageContractMaterialized contentName='{phaseName}' hasRunResultStage='{hasRunResultStage}' source='GameplayPhaseRuntime'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageSessionUpdated rail='phase' contentName='{phaseName}' hasIntroStage='{session.HasIntroStage}' hasRunResultStage='{session.HasRunResultStage}' v='{session.SelectionVersion}' entrySeq='{session.PhaseLocalEntrySequence}' signature='{session.SessionSignature}' entrySignature='{session.EntrySignature}' reason='{session.Reason}' source='{evt.Source}'.",
                DebugUtility.Colors.Info);
        }
    }
}
