using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Runtime
{
    /// <summary>
    /// Etapas assíncronas de reset para componentes de gameplay.
    /// </summary>
    public enum RunRearmStep
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }

    /// <summary>
    /// Alvo/escopo suportado para reset de gameplay.
    /// </summary>
    public enum RunRearmTarget
    {
        AllActorsInScene = 0,
        PlayersOnly = 1,
        EaterOnly = 2,
        ActorIdSet = 3,
        ByActorKind = 4
    }

    /// <summary>
    /// Pedido de reset contextualizado com alvo e actorIds (quando aplicável).
    /// </summary>
    public readonly struct RunRearmRequest
    {
        public RunRearmRequest(
            RunRearmTarget target,
            string reason = null,
            IReadOnlyList<string> actorIds = null,
            ActorKind actorKind = ActorKind.Unknown)
        {
            Target = target;
            Reason = reason;
            ActorIds = actorIds;
            ActorKind = actorKind;
        }

        public RunRearmTarget Target { get; }

        public string Reason { get; }

        public IReadOnlyList<string> ActorIds { get; }

        public ActorKind ActorKind { get; }

        public static RunRearmRequest ByActorKind(ActorKind kind, string reason = null)
        {
            return new RunRearmRequest(RunRearmTarget.ByActorKind, reason, actorKind: kind);
        }

        public override string ToString()
        {
            int count = ActorIds?.Count ?? 0;
            string kindInfo = Target == RunRearmTarget.ByActorKind || Target == RunRearmTarget.PlayersOnly
                ? $", ActorKind={ActorKind}"
                : string.Empty;
            return $"RunRearmRequest(Target={Target}, Reason='{Reason ?? "null"}', ActorIds={count}{kindInfo})";
        }
    }

    /// <summary>
    /// Contexto corrente de reset, compartilhado entre etapas e participantes.
    /// </summary>
    public readonly struct RunRearmContext
    {
        public RunRearmContext(
            string sceneName,
            RunRearmRequest request,
            int requestSerial,
            int frameStarted,
            RunRearmStep currentStep)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            CurrentStep = currentStep;
        }

        public string SceneName { get; }

        public RunRearmRequest Request { get; }

        public int RequestSerial { get; }

        public int FrameStarted { get; }

        public RunRearmStep CurrentStep { get; }

        public RunRearmContext WithStep(RunRearmStep step)
        {
            return new RunRearmContext(SceneName, Request, RequestSerial, FrameStarted, step);
        }

        public override string ToString()
        {
            return $"RunRearmContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Step={CurrentStep}, {Request})";
        }
    }

    /// <summary>
    /// Participante assíncrono de reset de gameplay (recomendado).
    /// </summary>
    public interface IRunRearmable
    {
        Task ResetCleanupAsync(RunRearmContext ctx);
        Task ResetRestoreAsync(RunRearmContext ctx);
        Task ResetRebindAsync(RunRearmContext ctx);
    }

    /// <summary>
    /// Participante síncrono (fallback). Um orchestrator pode adaptar para Task.
    /// </summary>
    public interface IRunRearmableSync
    {
        void ResetCleanup(RunRearmContext ctx);
        void ResetRestore(RunRearmContext ctx);
        void ResetRebind(RunRearmContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada etapa. Menor primeiro.
    /// </summary>
    public interface IRunRearmOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos alvos/escopos.
    /// </summary>
    public interface IRunRearmTargetFilter
    {
        bool ShouldParticipate(RunRearmTarget target);
    }

    public interface IRunRearmOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(RunRearmRequest request);
    }
}
