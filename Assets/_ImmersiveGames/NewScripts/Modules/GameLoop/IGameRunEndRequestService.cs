// Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/IGameRunEndRequestService.cs

namespace _ImmersiveGames.NewScripts.Modules.GameLoop
{
    /// <summary>
    /// Ponto único de entrada para solicitar o encerramento de uma execução de gameplay (vitória/derrota).
    ///
    /// Observações:
    /// - Este serviço NÃO define as condições de vitória/derrota.
    /// - Sistemas de gameplay devem apenas chamar <see cref="RequestEnd"/> quando detectarem
    ///   suas próprias condições (tempo, morte, objetivo, sequência de eventos etc.).
    /// - O processamento real e a deduplicação acontecem do lado consumidor (ex.: GameRunOutcomeService).
    /// </summary>
    public interface IGameRunEndRequestService
    {
        /// <summary>
        /// Solicita o encerramento do run atual.
        /// </summary>
        /// <param name="outcome">Vitória ou Derrota.</param>
        /// <param name="reason">Motivo/assinatura do emissor (útil para debug/telemetria).</param>
        void RequestEnd(GameRunOutcome outcome, string reason);
    }
}
