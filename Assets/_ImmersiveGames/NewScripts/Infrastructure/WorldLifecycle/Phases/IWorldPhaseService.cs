namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Serviço global que mantém o estado de fase atual e a próxima fase solicitada.
    /// </summary>
    public interface IWorldPhaseService
    {
        PhaseId CurrentPhaseId { get; }

        PhaseId? RequestedPhaseId { get; }

        int Epoch { get; }

        int? Seed { get; }

        PhaseSnapshot CaptureSnapshot(string reason);

        PhaseSnapshot CommitRequestedPhase(string reason);

        void RequestPhase(PhaseId phaseId, string reason);

        void RestartPhase(string reason);
    }
}
