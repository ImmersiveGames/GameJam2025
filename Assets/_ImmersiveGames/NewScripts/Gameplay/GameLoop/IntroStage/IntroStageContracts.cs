#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Contexto mínimo para execução da IntroStage antes da revelação da cena.
    /// </summary>
    public readonly struct IntroStageContext
    {
        public string ContextSignature { get; }
        public SceneFlowProfileId ProfileId { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public IntroStageContext(string contextSignature, SceneFlowProfileId profileId, string targetScene, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            ProfileId = profileId;
            TargetScene = targetScene ?? string.Empty;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>
    /// Política de execução da IntroStage.
    /// </summary>
    public enum IntroStagePolicy
    {
        Disabled,
        Manual,
        AutoComplete
    }

    /// <summary>
    /// Resolve a política de execução da IntroStage para um contexto.
    /// </summary>
    public interface IIntroStagePolicyResolver
    {
        IntroStagePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason);
    }

    /// <summary>
    /// Passo de IntroStage (opcional). Deve concluir sem bloquear o fluxo.
    /// </summary>
    public interface IIntroStageStep
    {
        bool HasContent { get; }
        Task RunAsync(IntroStageContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Coordenador que executa a IntroStage usando o IIntroStageStep disponível.
    /// </summary>
    public interface IIntroStageCoordinator
    {
        Task RunIntroStageAsync(IntroStageContext context);
    }

    /// <summary>
    /// Serviço global para controlar a conclusão da IntroStage via comando explícito.
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
    /// Resultado final da IntroStage (com razão e indicação de skip).
    /// </summary>
    public readonly struct IntroStageCompletionResult
    {
        public string Reason { get; }
        public bool WasSkipped { get; }

        public IntroStageCompletionResult(string reason, bool wasSkipped)
        {
            Reason = reason ?? string.Empty;
            WasSkipped = wasSkipped;
        }
    }

    /// <summary>
    /// Implementação default (no-op) para ausência de conteúdo.
    /// </summary>
    public sealed class NoOpIntroStageStep : IIntroStageStep, IPregameStep
    {
        public bool HasContent => false;

        public Task RunAsync(IntroStageContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task IPregameStep.RunAsync(PregameContext context, CancellationToken cancellationToken)
            => RunAsync(context.ToIntroStageContext(), cancellationToken);

        bool IPregameStep.HasContent => HasContent;
    }

    [Obsolete("Use IntroStageContext. Será removido após a migração para IntroStage.")]
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

        public IntroStageContext ToIntroStageContext()
            => new IntroStageContext(ContextSignature, ProfileId, TargetScene, Reason);
    }

    [Obsolete("Use IntroStagePolicy. Será removido após a migração para IntroStage.")]
    public enum PregamePolicy
    {
        Disabled,
        Manual,
        AutoComplete
    }

    [Obsolete("Use IIntroStagePolicyResolver. Será removido após a migração para IntroStage.")]
    public interface IPregamePolicyResolver
    {
        PregamePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason);
    }

    [Obsolete("Use IIntroStageStep. Será removido após a migração para IntroStage.")]
    public interface IPregameStep
    {
        bool HasContent { get; }
        Task RunAsync(PregameContext context, CancellationToken cancellationToken);
    }

    [Obsolete("Use IIntroStageCoordinator. Será removido após a migração para IntroStage.")]
    public interface IPregameCoordinator
    {
        Task RunPregameAsync(PregameContext context);
    }

    [Obsolete("Use IIntroStageControlService. Será removido após a migração para IntroStage.")]
    public interface IPregameControlService
    {
        bool IsPregameActive { get; }
        void BeginPregame(PregameContext context);
        Task<PregameCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken);
        void CompletePregame(string reason);
        void SkipPregame(string reason);
    }

    [Obsolete("Use IntroStageCompletionResult. Será removido após a migração para IntroStage.")]
    public readonly struct PregameCompletionResult
    {
        public string Reason { get; }
        public bool WasSkipped { get; }

        public PregameCompletionResult(string reason, bool wasSkipped)
        {
            Reason = reason ?? string.Empty;
            WasSkipped = wasSkipped;
        }

        public IntroStageCompletionResult ToIntroStageCompletionResult()
            => new IntroStageCompletionResult(Reason, WasSkipped);
    }

    [Obsolete("Use NoOpIntroStageStep. Será removido após a migração para IntroStage.")]
    public sealed class NoOpPregameStep : IPregameStep
    {
        public bool HasContent => false;

        public Task RunAsync(PregameContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
