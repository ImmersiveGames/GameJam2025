using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalRuntimeState
    {
        private readonly List<SessionOperationalFact> _facts = new();
        private readonly List<string> _trace = new();

        public string SessionOperationalPipelineId { get; internal set; }
        public string RouteIdentity { get; internal set; }
        public string RouteOperationId { get; internal set; }
        public string TransitionId { get; internal set; }
        public int TransitionSequence { get; internal set; }
        public string RouteId { get; internal set; }
        public string RouteProfileId { get; internal set; }
        public string RouteClass { get; internal set; }
        public SessionOperationalInputPolicy CurrentInputPolicy { get; internal set; }
        public SessionOperationalInputModeKind CurrentInitialInputMode { get; internal set; }
        public bool HasStarted { get; internal set; }
        public bool HasCompleted { get; internal set; }
        public SessionOperationalStage CurrentStage { get; internal set; }
        public SessionOperationalIdentity CurrentIdentity { get; internal set; }

        public IReadOnlyList<SessionOperationalFact> Facts => _facts;
        public IReadOnlyList<string> Trace => _trace;

        public void Reset(
            string sessionOperationalPipelineId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId)
        {
            SessionOperationalPipelineId = sessionOperationalPipelineId;
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            TransitionSequence = transitionSequence;
            RouteId = routeId;
            RouteProfileId = routeProfileId;
            RouteClass = string.Empty;
            CurrentInputPolicy = SessionOperationalInputPolicy.Unknown;
            CurrentInitialInputMode = SessionOperationalInputModeKind.Unknown;
            HasStarted = false;
            HasCompleted = false;
            CurrentStage = SessionOperationalStage.Unknown;
            CurrentIdentity = default;
            _facts.Clear();
            _trace.Clear();
        }

        public void MarkStarted()
        {
            HasStarted = true;
        }

        public void MarkCompleted()
        {
            HasCompleted = true;
        }

        public void SetCurrentIdentity(SessionOperationalIdentity identity)
        {
            CurrentIdentity = identity;
            CurrentStage = identity.Stage;
            TransitionSequence = identity.TransitionSequence;
            RouteIdentity = identity.RouteIdentity;
            RouteId = identity.RouteId;
            RouteProfileId = identity.RouteProfileId;
            RouteOperationId = identity.RouteOperationId;
            TransitionId = identity.TransitionId;
        }

        public void SetInputModeContext(
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode)
        {
            RouteClass = routeClass;
            CurrentInputPolicy = inputPolicy;
            CurrentInitialInputMode = initialInputMode;
        }

        public void AppendFact(SessionOperationalFact fact)
        {
            _facts.Add(fact);
        }

        public void AppendTrace(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _trace.Add(line);
            }
        }
    }
}
