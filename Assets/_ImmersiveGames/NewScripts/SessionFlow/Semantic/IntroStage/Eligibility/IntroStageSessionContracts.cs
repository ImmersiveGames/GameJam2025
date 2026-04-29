#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
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
        private readonly EventBinding<GameplayPhaseRuntimeMaterializedEvent> _phaseRuntimeMaterializedBinding;
        private readonly IIntroStagePresenterScopeResolver _presenterScopeResolver;
        private IntroStageSession _currentSession;
        private bool _disposed;

        public IntroStageSessionService()
            : this(ResolvePresenterScopeResolverOrFail())
        {
        }

        public IntroStageSessionService(IIntroStagePresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
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

        private static IIntroStagePresenterScopeResolver ResolvePresenterScopeResolverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePresenterScopeResolver>(out var resolver) && resolver != null)
            {
                return resolver;
            }

            throw new InvalidOperationException("[FATAL][Config][IntroStage] IIntroStagePresenterScopeResolver obrigatorio ausente para IntroStageSessionService.");
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

            string localContentId = PhaseDefinitionId.BuildCanonicalIntroContentId(phaseDefinitionRef.PhaseId);

            // Create temporary session to resolve operationally.
            IntroStageSession tempSession = new IntroStageSession(
                phaseDefinitionRef,
                localContentId,
                runtime.SessionContext.Reason,
                runtime.SessionContext.SelectionVersion,
                evt.PhaseLocalEntrySequence,
                runtime.SessionContext.SessionSignature,
                hasIntroStage: false,
                phaseRuntimeSignature: runtime.PhaseRuntimeSignature); // Start with false; resolver will determine.

            // Operationally resolve: does a valid presenter exist in the phase scope?
            bool hasIntroStage = _presenterScopeResolver.TryResolvePresenters(tempSession, out _);

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageContractMaterialized contentName='{phaseName}' source='GameplayPhaseRuntime' sessionSignature='{runtime.SessionContext.SessionSignature}' phaseRuntimeSignature='{runtime.PhaseRuntimeSignature}' hasIntroStage='{hasIntroStage}' resolvedViaOperationalContract='true'.",
                DebugUtility.Colors.Info);

            IntroStageSession session = runtime.CreateIntroStageSession(
                localContentId,
                runtime.SessionContext.Reason,
                runtime.SessionContext.SelectionVersion,
                evt.PhaseLocalEntrySequence,
                runtime.SessionContext.SessionSignature,
                runtime.PhaseRuntimeSignature,
                evt.EntrySignature);

            // Override the session with resolved HasIntroStage.
            IntroStageSession resolvedSession = new IntroStageSession(
                session.PhaseDefinitionRef,
                session.LocalContentId,
                session.Reason,
                session.SelectionVersion,
                session.PhaseLocalEntrySequence,
                session.SessionSignature,
                hasIntroStage,
                session.EntrySignature,
                session.PhaseRuntimeSignature);

            lock (_sync)
            {
                _currentSession = resolvedSession;
            }

            DebugUtility.Log<IntroStageSessionService>(
                $"[OBS][IntroStage] IntroStageSessionUpdated rail='operational_contract_resolution' contentName='{phaseName}' hasIntroStage='{resolvedSession.HasIntroStage}' v='{resolvedSession.SelectionVersion}' entrySeq='{resolvedSession.PhaseLocalEntrySequence}' sessionSignature='{resolvedSession.SessionSignature}' phaseRuntimeSignature='{resolvedSession.PhaseRuntimeSignature}' entrySignature='{resolvedSession.EntrySignature}' reason='{resolvedSession.Reason}' source='{evt.Source}'.",
                DebugUtility.Colors.Info);
        }
    }
}

