using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityRuntimeState
    {
        private readonly List<SessionActivityFact> _facts = new();
        private readonly List<SessionActivitySnapshot> _snapshots = new();
        private readonly List<string> _trace = new();

        public string PipelineId { get; internal set; }
        public string SessionId { get; internal set; }
        public bool HasStarted { get; internal set; }
        public bool HasCompleted { get; internal set; }
        public SessionActivityDefinition CurrentDefinition { get; internal set; }
        public SessionActivityIdentity CurrentIdentity { get; internal set; }
        public SessionActivityStage CurrentStage { get; internal set; }
        public SessionActivityHandoff CurrentHandoff { get; internal set; }
        public int CurrentActivityIndex { get; internal set; }
        public int CurrentEntrySequence { get; internal set; }
        public int CatalogLoopCount { get; internal set; }
        public ActivityExecutionState CurrentExecutionState { get; internal set; }
        public SessionActivityPendingOperation CurrentPendingOperation { get; internal set; }
        public ActivityContentLoadedSet CurrentActivityContentLoadedSet { get; internal set; }
        public ActivityObjectContributorDiscoveryResult CurrentActivityObjectContributorDiscoveryResult { get; internal set; }
        public ActivitySetupInventory CurrentActivitySetupInventory { get; internal set; }
        public ActivityCapabilityInventory CurrentActivityCapabilityInventoryPreview { get; internal set; }
        public ActivityCapabilityInventoryValidationResult CurrentActivityCapabilityInventoryPreviewValidation { get; internal set; }

        public IReadOnlyList<SessionActivityFact> Facts => _facts;
        public IReadOnlyList<SessionActivitySnapshot> Snapshots => _snapshots;
        public IReadOnlyList<string> Trace => _trace;

        public void Reset(string pipelineId, string sessionStateId)
        {
            PipelineId = pipelineId;
            SessionId = sessionStateId;
            HasStarted = false;
            HasCompleted = false;
            CurrentDefinition = default;
            CurrentIdentity = default;
            CurrentStage = SessionActivityStage.Unknown;
            CurrentHandoff = default;
            CurrentActivityIndex = 0;
            CurrentEntrySequence = 0;
            CatalogLoopCount = 0;
            CurrentExecutionState = ActivityExecutionState.Stopped;
            CurrentPendingOperation = default;
            CurrentActivityContentLoadedSet = default;
            CurrentActivityObjectContributorDiscoveryResult = default;
            CurrentActivitySetupInventory = default;
            CurrentActivityCapabilityInventoryPreview = default;
            CurrentActivityCapabilityInventoryPreviewValidation = default;
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

        public void SetCurrentDefinition(SessionActivityDefinition definition)
        {
            CurrentDefinition = definition;
            CurrentActivityIndex = definition.ActivityOrdinal;
        }

        public void SetCurrentIdentity(SessionActivityIdentity identity, SessionActivityStage stage)
        {
            CurrentIdentity = identity;
            CurrentStage = stage;
            CurrentEntrySequence = identity.EntrySequence;
        }

        public void SetHandoff(SessionActivityHandoff handoff)
        {
            CurrentHandoff = handoff;
        }

        public void SetExecutionState(ActivityExecutionState executionState)
        {
            CurrentExecutionState = executionState;
        }

        public void SetPendingOperation(SessionActivityPendingOperation operation)
        {
            CurrentPendingOperation = operation;
        }

        public void ClearPendingOperation()
        {
            CurrentPendingOperation = default;
        }

        public void SetCurrentActivityContentLoadedSet(ActivityContentLoadedSet loadedSet)
        {
            CurrentActivityContentLoadedSet = loadedSet;
        }

        public void ClearCurrentActivityContentLoadedSet()
        {
            CurrentActivityContentLoadedSet = default;
        }

        public void SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result)
        {
            CurrentActivityObjectContributorDiscoveryResult = result;
        }

        public void ClearCurrentActivityObjectContributorDiscoveryResult()
        {
            CurrentActivityObjectContributorDiscoveryResult = default;
            CurrentActivityCapabilityInventoryPreview = default;
            CurrentActivityCapabilityInventoryPreviewValidation = default;
        }

        public void SetCurrentActivitySetupInventory(ActivitySetupInventory inventory)
        {
            CurrentActivitySetupInventory = inventory;
        }

        public void ClearCurrentActivitySetupInventory()
        {
            CurrentActivitySetupInventory = default;
        }

        public void SetCurrentActivityCapabilityInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation)
        {
            CurrentActivityCapabilityInventoryPreview = inventory;
            CurrentActivityCapabilityInventoryPreviewValidation = validation;
        }

        public void ClearCurrentActivityCapabilityInventoryPreview()
        {
            CurrentActivityCapabilityInventoryPreview = default;
            CurrentActivityCapabilityInventoryPreviewValidation = default;
        }

        public void IncrementCatalogLoopCount()
        {
            CatalogLoopCount++;
        }

        public void ClearHandoff()
        {
            CurrentHandoff = default;
        }

        public void AppendFact(SessionActivityFact fact)
        {
            _facts.Add(fact);
        }

        public void AppendSnapshot(SessionActivitySnapshot snapshot)
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
