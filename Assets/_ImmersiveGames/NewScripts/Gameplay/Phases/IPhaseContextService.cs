#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// Mantém fase atual e uma fase "marcada" (pending) para aplicar em um ponto seguro do fluxo.
    /// </summary>
    public interface IPhaseContextService
    {
        PhasePlan Current { get; }
        PhasePlan Pending { get; }
        bool HasPending { get; }

        /// <summary>Marca uma fase para ser aplicada mais tarde (não aplica imediatamente).</summary>
        void SetPending(PhasePlan plan, string reason);

        /// <summary>
        /// Converte a fase pending em fase atual. Deve ser chamado apenas em um ponto seguro (após ScenesReady + ResetCompleted).
        /// Retorna true se houve commit.
        /// </summary>
        bool TryCommitPending(string reason, out PhasePlan committed);

        /// <summary>Limpa a fase pending sem aplicar.</summary>
        void ClearPending(string reason);
    }
}
