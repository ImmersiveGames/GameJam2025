using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate
{
    /// <summary>
    /// Controla se a "simulação" (lógica de gameplay) deve rodar ou não,
    /// sem depender de Time.timeScale.
    ///
    /// Abertura/fechamento é controlado por "tokens" (strings).
    /// - Se existir pelo menos 1 token ativo → Gate FECHADO (simulação bloqueada)
    /// - Se não existir token → Gate ABERTO (simulação liberada)
    ///
    /// Padrão recomendado: cada sistema/domínio/estado adquire um token no OnEnter
    /// e libera no OnExit (ou via IDisposable).
    /// </summary>
    public interface ISimulationGateService
    {
        /// <summary>Gate está aberto (nenhum token ativo)?</summary>
        bool IsOpen { get; }

        /// <summary>Quantidade de tokens ativos.</summary>
        int ActiveTokenCount { get; }

        /// <summary>
        /// Evento disparado quando o gate muda de estado (abriu/fechou).
        /// bool => IsOpen.
        /// </summary>
        event Action<bool> GateChanged;

        /// <summary>
        /// Adquire um token. Enquanto token estiver ativo, gate fica fechado.
        /// Retorna um handle (IDisposable) que libera o token ao dar Dispose().
        /// </summary>
        IDisposable Acquire(string token);

        /// <summary>
        /// Libera um token adquirido.
        /// </summary>
        void Release(string token);

        /// <summary>
        /// Apenas consulta: token está ativo?
        /// </summary>
        bool IsTokenActive(string token);
    }
}
