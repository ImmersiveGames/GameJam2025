#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage
{
    /// <summary>
    /// Contexto mínimo para execução da IntroStageController antes da revelação da cena.
    /// </summary>
    public readonly struct IntroStageContext
    {
        public string ContextSignature { get; }
        public SceneFlowProfileId ProfileId { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public IntroStageContext(string? contextSignature, SceneFlowProfileId profileId, string? targetScene, string? reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            ProfileId = profileId;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    /// Política de execução da IntroStageController.
    /// </summary>
    public enum IntroStagePolicy
    {
        Disabled,
        Manual,
        AutoComplete
    }

    /// <summary>
    /// Resolve a política de execução da IntroStageController para um contexto.
    /// </summary>
    public interface IIntroStagePolicyResolver
    {
        IntroStagePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason);
    }

    /// <summary>
    /// Passo de IntroStageController (opcional). Deve concluir sem bloquear o fluxo.
    /// </summary>
    public interface IIntroStageStep
    {
        bool HasContent { get; }
        Task RunAsync(IntroStageContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Coordenador que executa a IntroStageController usando o IIntroStageStep disponível.
    /// </summary>
    public interface IIntroStageCoordinator
    {
        Task RunIntroStageAsync(IntroStageContext context);
    }

    /// <summary>
    /// Serviço global para controlar a conclusão da IntroStageController via comando explícito.
    /// </summary>
    public interface IIntroStageControlService
    {
        bool IsIntroStageActive { get; }
        void BeginIntroStage(IntroStageContext context);
        Task<IntroStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken);
        void CompleteIntroStage(string reason);
        void SkipIntroStage(string reason);
    }

    /// <summary>
    /// Resultado da IntroStageController (com razão e indicação de skip).
    /// </summary>
    public readonly struct IntroStageCompletionResult
    {
        public string Reason { get; }
        public bool WasSkipped { get; }

        public IntroStageCompletionResult(string? reason, bool wasSkipped)
        {
            Reason = reason ?? string.Empty;
            WasSkipped = wasSkipped;
        }
    }

    /// <summary>
    /// Implementação default (no-op) para ausência de conteúdo.
    /// </summary>
    public sealed class NoOpIntroStageStep : IIntroStageStep
    {
        public bool HasContent => false;

        public Task RunAsync(IntroStageContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
