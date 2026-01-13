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
    /// Implementação default (no-op) para ausência de conteúdo.
    /// </summary>
    public sealed class NoOpPregameStep : IPregameStep
    {
        public bool HasContent => false;

        public Task RunAsync(PregameContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
