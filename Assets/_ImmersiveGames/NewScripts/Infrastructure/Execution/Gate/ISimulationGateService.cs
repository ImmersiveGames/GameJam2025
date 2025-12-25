using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate
{
    /// <summary>
    /// Controla se a "simulação" (lógica de gameplay) deve rodar ou não,
    /// sem depender de Time.timeScale.
    ///
    /// Abertura/fechamento é controlado por tokens (strings).
    /// - Se existir pelo menos 1 token ativo → Gate FECHADO (simulação bloqueada)
    /// - Se não existir token → Gate ABERTO (simulação liberada)
    ///
    /// Padrão recomendado:
    /// - Infra (SceneFlow/Reset/Pause/etc.) adquire token no OnEnter e libera no OnExit.
    /// - Preferir o handle retornado por Acquire(token) para garantir Dispose() determinístico.
    ///
    /// Observação de arquitetura:
    /// - O Gate é infraestrutura (não domínio). Ele NÃO decide "se uma ação pode acontecer";
    ///   ele apenas sinaliza se a simulação está bloqueada por alguma condição infra.
    /// </summary>
    public interface ISimulationGateService
    {
        /// <summary>Gate está aberto (nenhum token ativo)?</summary>
        bool IsOpen { get; }

        /// <summary>Quantidade de tokens ativos (distintos).</summary>
        int ActiveTokenCount { get; }

        /// <summary>
        /// Evento disparado quando o gate muda de estado (abriu/fechou).
        /// bool => IsOpen.
        /// </summary>
        event Action<bool> GateChanged;

        /// <summary>
        /// Adquire um token. Enquanto token estiver ativo, gate fica fechado.
        ///
        /// Retorna um handle (IDisposable) que libera o token ao dar Dispose().
        /// Implementações robustas devem suportar aquisições repetidas do MESMO token
        /// (ref-count), para evitar "liberação prematura" quando há múltiplos donos.
        /// </summary>
        IDisposable Acquire(string token);

        /// <summary>
        /// Libera um token.
        /// Observação: chamada direta é útil como fallback quando não há handle,
        /// mas preferir Dispose() do handle quando possível.
        /// </summary>
        void Release(string token);

        /// <summary>
        /// Apenas consulta: token está ativo?
        /// </summary>
        bool IsTokenActive(string token);
    }
}
