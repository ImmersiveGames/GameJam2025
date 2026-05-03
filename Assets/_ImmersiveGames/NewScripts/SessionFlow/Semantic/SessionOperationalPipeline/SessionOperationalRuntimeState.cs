using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public sealed class SessionOperationalRuntimeState
    {
        private readonly List<SessionOperationalFact> _facts = new();
        private readonly List<SessionOperationalSnapshot> _snapshots = new();
        private readonly List<string> _trace = new();

        public string SessionPipelineId { get; internal set; }
        public string SessionStateId { get; internal set; }
        public string RouteId { get; internal set; }
        public string RouteProfileId { get; internal set; }
        public int TransitionSequence { get; internal set; }
        public bool HasStarted { get; internal set; }
        public bool HasCompleted { get; internal set; }
        public SessionOperationalStage CurrentStage { get; internal set; }
        public SessionOperationalIdentity CurrentIdentity { get; internal set; }
        public SessionActivityDefinition InitialActivity { get; internal set; }
        public SessionActivityEntryHandoff CurrentActivityEntryHandoff { get; internal set; }
        public SessionOperationalResult LastResult { get; internal set; }
        public SessionActivityCommandResult ActivityStartResult { get; internal set; }

        public IReadOnlyList<SessionOperationalFact> Facts => _facts;
        public IReadOnlyList<SessionOperationalSnapshot> Snapshots => _snapshots;
        public IReadOnlyList<string> Trace => _trace;

        public void Reset(
            string sessionPipelineId,
            string sessionStateId,
            string routeId,
            string routeProfileId,
            int transitionSequence)
        {
            SessionPipelineId = sessionPipelineId;
            SessionStateId = sessionStateId;
            RouteId = routeId;
            RouteProfileId = routeProfileId;
            TransitionSequence = transitionSequence;
            HasStarted = false;
            HasCompleted = false;
            CurrentStage = SessionOperationalStage.Unknown;
            CurrentIdentity = default;
            InitialActivity = default;
            CurrentActivityEntryHandoff = default;
            LastResult = default;
            ActivityStartResult = default;
            _facts.Clear();
            _snapshots.Clear();
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

        public void SetIdentity(SessionOperationalIdentity identity, SessionOperationalStage stage)
        {
            CurrentIdentity = identity;
            CurrentStage = stage;
            TransitionSequence = identity.TransitionSequence;
        }

        public void SetInitialActivity(SessionActivityDefinition definition)
        {
            InitialActivity = definition;
        }

        public void SetActivityEntryHandoff(SessionActivityEntryHandoff handoff)
        {
            CurrentActivityEntryHandoff = handoff;
        }

        public void SetActivityStartResult(SessionActivityCommandResult result)
        {
            ActivityStartResult = result;
        }

        public void SetResult(SessionOperationalResult result)
        {
            LastResult = result;
        }

        public void AppendFact(SessionOperationalFact fact)
        {
            _facts.Add(fact);
        }

        public void AppendSnapshot(SessionOperationalSnapshot snapshot)
        {
            _snapshots.Add(snapshot);
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
