using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalRuntimeState
    {
        private readonly List<SessionOperationalFact> _facts = new();
        private readonly List<string> _trace = new();

        public string SessionOperationalPipelineId { get; internal set; }
        public string RouteOperationId { get; internal set; }
        public string TransitionId { get; internal set; }
        public int TransitionSequence { get; internal set; }
        public string RouteId { get; internal set; }
        public string RouteProfileId { get; internal set; }
        public string RouteClass { get; internal set; }
        public SessionOperationalInputModeKind CurrentInitialInputMode { get; internal set; }
        public bool HasStarted { get; internal set; }
        public bool HasCompleted { get; internal set; }
        public SessionOperationalStage CurrentStage { get; internal set; }
        public SessionOperationalIdentity CurrentIdentity { get; internal set; }

        public IReadOnlyList<SessionOperationalFact> Facts => _facts;
        public IReadOnlyList<string> Trace => _trace;

        public void Reset(
            string sessionOperationalPipelineId,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId)
        {
            SessionOperationalPipelineId = sessionOperationalPipelineId;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            TransitionSequence = transitionSequence;
            RouteId = routeId;
            RouteProfileId = routeProfileId;
            RouteClass = string.Empty;
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
            RouteId = identity.RouteId;
            RouteProfileId = identity.RouteProfileId;
            RouteOperationId = identity.RouteOperationId;
            TransitionId = identity.TransitionId;
        }

        public void SetInputModeContext(string routeClass, SessionOperationalInputModeKind initialInputMode)
        {
            RouteClass = routeClass;
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

