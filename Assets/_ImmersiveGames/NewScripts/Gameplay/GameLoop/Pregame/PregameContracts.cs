#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Contexto mínimo para execução do pregame antes da revelação da cena.
    /// </summary>
    public readonly struct PregameContext
    {
        public string ContextSignature { get; }
        public SceneFlowProfileId ProfileId { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public PregameContext(string contextSignature, SceneFlowProfileId profileId, string targetScene, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            ProfileId = profileId;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    /// Política de execução do Pregame.
    /// </summary>
    public enum PregamePolicy
    {
        Disabled,
        Manual,
        AutoComplete
    }

    /// <summary>
    /// Resolve a política de execução do Pregame para um contexto.
    /// </summary>
    public interface IPregamePolicyResolver
    {
        PregamePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason);
    }

    /// <summary>
    /// Passo de pregame (opcional). Deve concluir sem bloquear o fluxo.
    /// </summary>
    public interface IPregameStep
    {
        bool HasContent { get; }
        Task RunAsync(PregameContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Coordenador que executa o pregame usando o IPregameStep disponível.
    /// </summary>
    public interface IPregameCoordinator
    {
        Task RunPregameAsync(PregameContext context);
    }

    /// <summary>
    /// Serviço global para controlar a conclusão do Pregame via comando explícito.
    /// </summary>
    public interface IPregameControlService
    {
        bool IsPregameActive { get; }
        void BeginPregame(PregameContext context);
        Task<PregameCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken);
        void CompletePregame(string reason);
        void SkipPregame(string reason);
    }

    /// <summary>
    /// Resultado final do Pregame (com razão e indicação de skip).
    /// </summary>
    public readonly struct PregameCompletionResult
    {
        public string Reason { get; }
        public bool WasSkipped { get; }

        public PregameCompletionResult(string reason, bool wasSkipped)
        {
            Reason = reason ?? string.Empty;
            WasSkipped = wasSkipped;
        }
    }

    /// <summary>
    /// Implementação default (no-op) para ausência de conteúdo.
    /// </summary>
    public sealed class NoOpPregameStep : IPregameStep
    {
        public bool HasContent => false;

        public Task RunAsync(PregameContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
