using System;
namespace _ImmersiveGames.NewScripts.Modules.Gates
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
    /// Importante (contrato):
    /// - Acquire suporta múltiplas aquisições do MESMO token (ref-count).
    /// - Release(token) libera UMA aquisição (equivalente a "ReleaseOne").
    /// - ReleaseAll(token) remove completamente o token (força cleanup) e deve ser usado apenas
    ///   em casos excepcionais (QA/emergência) porque pode mascarar vazamentos de handle.
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
        /// Retorna um handle (IDisposable) que libera UMA aquisição ao dar Dispose().
        /// Implementações robustas devem suportar aquisições repetidas do MESMO token (ref-count),
        /// para evitar "liberação prematura" quando há múltiplos donos.
        /// </summary>
        IDisposable Acquire(string token);

        /// <summary>
        /// Libera UMA aquisição do token (ref-count).
        /// Preferir Dispose() do handle quando possível.
        /// </summary>
        void Release(string token);

        /// <summary>
        /// Remove completamente o token (zera todas as aquisições daquele token).
        /// Use apenas em QA/emergência; em produção prefira o fluxo por handle/ref-count.
        /// </summary>
        void ReleaseAll(string token);

        /// <summary>
        /// Apenas consulta: token está ativo?
        /// </summary>
        bool IsTokenActive(string token);
    }
}
