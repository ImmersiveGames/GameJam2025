namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Snapshot imutável da fase atual (capturado no início do reset).
    /// </summary>
    public readonly struct PhaseSnapshot
    {
        public PhaseSnapshot(PhaseId currentPhaseId, PhaseId? requestedPhaseId, int epoch, int? seed)
        {
            CurrentPhaseId = currentPhaseId;
            RequestedPhaseId = requestedPhaseId;
            Epoch = epoch;
            Seed = seed;
        }

        public PhaseId CurrentPhaseId { get; }

        public PhaseId? RequestedPhaseId { get; }

        public int Epoch { get; }

        public int? Seed { get; }

        public string RequestedPhaseLabel => RequestedPhaseId.HasValue ? RequestedPhaseId.Value.Value : "<null>";
    }
}
