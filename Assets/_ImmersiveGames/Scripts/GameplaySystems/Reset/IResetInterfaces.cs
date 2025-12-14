using System.Threading.Tasks;

namespace _ImmersiveGames.Scripts.GameplaySystems.Reset
{
    /// <summary>
    /// Participante assíncrono de reset (recomendado).
    /// Deve ser idempotente por fase.
    /// </summary>
    public interface IResetInterfaces
    {
        Task Reset_CleanupAsync(ResetContext ctx);
        Task Reset_RestoreAsync(ResetContext ctx);
        Task Reset_RebindAsync(ResetContext ctx);
    }

    /// <summary>
    /// Participante síncrono (fallback). O Orchestrator adapta para Task.
    /// Deve ser idempotente por fase.
    /// </summary>
    public interface IResetParticipantSync
    {
        void Reset_Cleanup(ResetContext ctx);
        void Reset_Restore(ResetContext ctx);
        void Reset_Rebind(ResetContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada fase.
    /// Menor primeiro.
    /// </summary>
    public interface IResetOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos escopos.
    /// </summary>
    public interface IResetScopeFilter
    {
        bool ShouldParticipate(ResetScope scope);
    }
    
    public interface IResetOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(ResetRequest request);
    }
}