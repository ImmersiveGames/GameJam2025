using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public enum SessionActivityPhaseChangeCascadeStageDisposition
    {
        Unknown = 0,
        Execute = 1,
        SkipNoContent = 2
    }

    public readonly struct SessionActivityPhaseChangeCascadeResolution
    {
        public SessionActivityPhaseChangeCascadeResolution(
            string operation,
            PhaseOrdinalNavigationKind navigationKind,
            PhaseNavigationDirection direction,
            GameplayStartSnapshot currentSnapshot,
            GameplayPhaseRuntimeSnapshot currentPhaseRuntime,
            PhaseDefinitionAsset targetPhaseRef,
            string reason,
            string source,
            bool hasResultPresentationContract,
            SessionActivityPhaseChangeCascadeStageDisposition deactivationDisposition,
            SessionActivityPhaseChangeCascadeStageDisposition resultPresentationDisposition,
            SessionActivityPhaseChangeCascadeStageDisposition continuityDisposition)
        {
            Operation = Normalize(operation);
            NavigationKind = navigationKind;
            Direction = direction;
            CurrentSnapshot = currentSnapshot;
            CurrentPhaseRuntime = currentPhaseRuntime;
            TargetPhaseRef = targetPhaseRef;
            Reason = Normalize(reason);
            Source = Normalize(source);
            HasResultPresentationContract = hasResultPresentationContract;
            DeactivationDisposition = deactivationDisposition;
            ResultPresentationDisposition = resultPresentationDisposition;
            ContinuityDisposition = continuityDisposition;
        }

        public string Operation { get; }
        public PhaseOrdinalNavigationKind NavigationKind { get; }
        public PhaseNavigationDirection Direction { get; }
        public GameplayStartSnapshot CurrentSnapshot { get; }
        public GameplayPhaseRuntimeSnapshot CurrentPhaseRuntime { get; }
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public string Reason { get; }
        public string Source { get; }
        public bool HasResultPresentationContract { get; }
        public SessionActivityPhaseChangeCascadeStageDisposition DeactivationDisposition { get; }
        public SessionActivityPhaseChangeCascadeStageDisposition ResultPresentationDisposition { get; }
        public SessionActivityPhaseChangeCascadeStageDisposition ContinuityDisposition { get; }

        public PhaseEntryIdentity CurrentPhaseEntryIdentity => CurrentPhaseRuntime.PhaseEntryIdentity;
        public string SessionSignature => CurrentSnapshot.PhaseSignature;
        public string CurrentSessionSignature => SessionSignature;
        public string CurrentPhaseRuntimeSignature => CurrentPhaseRuntime.PhaseRuntimeSignature;
        public string CurrentPhaseSignature => CurrentPhaseRuntimeSignature;
        public string EntrySignature => CurrentPhaseEntryIdentity.EntrySignature;
        public string CurrentEntrySignature => EntrySignature;
        public int PhaseLocalEntrySequence => CurrentPhaseEntryIdentity.PhaseLocalEntrySequence;
        public SceneRouteId RouteId => CurrentSnapshot.MacroRouteId;
        public SceneRouteKind RouteKind => CurrentSnapshot.MacroRouteRef != null
            ? CurrentSnapshot.MacroRouteRef.RouteKind
            : SceneRouteKind.Unspecified;
        public string CurrentPhaseId => CurrentSnapshot.PhaseDefinitionRef != null && CurrentSnapshot.PhaseDefinitionRef.PhaseId.IsValid
            ? CurrentSnapshot.PhaseDefinitionRef.PhaseId.Value
            : string.Empty;
        public string TargetPhaseId => TargetPhaseRef != null && TargetPhaseRef.PhaseId.IsValid
            ? TargetPhaseRef.PhaseId.Value
            : string.Empty;
        public string TargetPhaseName => TargetPhaseRef != null ? TargetPhaseRef.name : string.Empty;
        public string PipelineHandoffTarget => "SessionTransition";
        public bool HasCurrentPhaseEntryIdentity => CurrentPhaseEntryIdentity.IsValid;
        public bool IsValid =>
            CurrentSnapshot.IsValid &&
            CurrentPhaseRuntime.IsValid &&
            HasCurrentPhaseEntryIdentity &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            TargetPhaseRef != null &&
            TargetPhaseRef.PhaseId.IsValid &&
            !string.IsNullOrWhiteSpace(Operation) &&
            !string.IsNullOrWhiteSpace(Reason) &&
            !string.IsNullOrWhiteSpace(Source) &&
            DeactivationDisposition != SessionActivityPhaseChangeCascadeStageDisposition.Unknown &&
            ResultPresentationDisposition != SessionActivityPhaseChangeCascadeStageDisposition.Unknown &&
            ContinuityDisposition != SessionActivityPhaseChangeCascadeStageDisposition.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class SessionActivityPhaseChangeCascadeResolvedEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeResolvedEvent(SessionActivityPhaseChangeCascadeResolution resolution)
        {
            Resolution = resolution;
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeDeactivationIntentResolvedEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeDeactivationIntentResolvedEvent(SessionActivityPhaseChangeCascadeResolution resolution)
        {
            Resolution = resolution;
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeResultPresentationResolvedEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeResultPresentationResolvedEvent(SessionActivityPhaseChangeCascadeResolution resolution)
        {
            Resolution = resolution;
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeResultPresentationSkippedNoContentEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeResultPresentationSkippedNoContentEvent(SessionActivityPhaseChangeCascadeResolution resolution, string detail)
        {
            Resolution = resolution;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public string Detail { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeResultPresentationEnteredEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeResultPresentationEnteredEvent(SessionActivityPhaseChangeCascadeResolution resolution, RunResultStage stage)
        {
            Resolution = resolution;
            Stage = stage;
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public RunResultStage Stage { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeResultPresentationCompletedEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeResultPresentationCompletedEvent(SessionActivityPhaseChangeCascadeResolution resolution, RunResultStage stage, RunResultStageCompletion completion)
        {
            Resolution = resolution;
            Stage = stage;
            Completion = completion;
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public RunResultStage Stage { get; }
        public RunResultStageCompletion Completion { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadeContinuityDecisionSkippedNoContentEvent(SessionActivityPhaseChangeCascadeResolution resolution, string detail)
        {
            Resolution = resolution;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public string Detail { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadePipelineHandoffReadyEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadePipelineHandoffReadyEvent(
            SessionActivityPhaseChangeCascadeResolution resolution,
            SessionTransitionPlan plan,
            string source)
        {
            Resolution = resolution;
            Plan = plan;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public SessionTransitionPlan Plan { get; }
        public string Source { get; }
    }

    public sealed class SessionActivityPhaseChangeCascadePipelineHandoffRequestedEvent : IEvent
    {
        public SessionActivityPhaseChangeCascadePipelineHandoffRequestedEvent(
            SessionActivityPhaseChangeCascadeResolution resolution,
            PhaseCatalogNavigationPlan navigationPlan,
            SessionTransitionPlan plan,
            string source)
        {
            Resolution = resolution;
            NavigationPlan = navigationPlan;
            Plan = plan;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public SessionActivityPhaseChangeCascadeResolution Resolution { get; }
        public PhaseCatalogNavigationPlan NavigationPlan { get; }
        public SessionTransitionPlan Plan { get; }
        public string Source { get; }
    }
}
