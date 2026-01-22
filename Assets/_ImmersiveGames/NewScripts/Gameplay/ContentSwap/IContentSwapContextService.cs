#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    /// <summary>
    /// Mantém conteúdo atual e um conteúdo "marcado" (pending) para aplicar em um ponto seguro do fluxo.
    /// </summary>
    public interface IContentSwapContextService
    {
        ContentSwapPlan Current { get; }
        ContentSwapPlan Pending { get; }
        bool HasPending { get; }

        /// <summary>Marca um conteúdo para ser aplicado mais tarde (não aplica imediatamente).</summary>
        void SetPending(ContentSwapPlan plan, string reason);

        /// <summary>
        /// Converte o conteúdo pending em conteúdo atual. Deve ser chamado apenas em um ponto seguro (após ScenesReady + ResetCompleted).
        /// Retorna true se houve commit.
        /// </summary>
        bool TryCommitPending(string reason, out ContentSwapPlan committed);

        /// <summary>Limpa o conteúdo pending sem aplicar.</summary>
        void ClearPending(string reason);
    }
}
