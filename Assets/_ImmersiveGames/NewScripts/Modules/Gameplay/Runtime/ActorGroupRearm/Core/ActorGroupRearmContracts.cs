using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    /// <summary>
    /// Etapas assíncronas de reset para componentes de gameplay.
    /// </summary>
    public enum ActorGroupRearmStep
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }

    /// <summary>
    /// Alvos canônicos suportados para reset de gameplay.
    /// </summary>
    public enum ActorGroupRearmTarget
    {
        ByActorKind = 0,
        ActorIdSet = 1
    }

    /// <summary>
    /// Pedido de reset contextualizado com alvo e actorIds (quando aplicável).
    /// </summary>
    public readonly struct ActorGroupRearmRequest
    {
        public ActorGroupRearmRequest(
            ActorGroupRearmTarget target,
            string reason = null,
            IReadOnlyList<string> actorIds = null,
            ActorKind actorKind = ActorKind.Unknown)
        {
            Target = target;
            Reason = reason;
            ActorIds = actorIds;
            ActorKind = actorKind;
        }

        public ActorGroupRearmTarget Target { get; }

        public string Reason { get; }

        public IReadOnlyList<string> ActorIds { get; }

        public ActorKind ActorKind { get; }

        public static ActorGroupRearmRequest ByActorKind(ActorKind kind, string reason = null)
        {
            return new ActorGroupRearmRequest(ActorGroupRearmTarget.ByActorKind, reason, actorKind: kind);
        }

        public static ActorGroupRearmRequest ForActorIds(IReadOnlyList<string> actorIds, string reason = null)
        {
            return new ActorGroupRearmRequest(ActorGroupRearmTarget.ActorIdSet, reason, actorIds: actorIds);
        }

        public override string ToString()
        {
            int count = ActorIds?.Count ?? 0;
            string kindInfo = Target == ActorGroupRearmTarget.ByActorKind
                ? $", ActorKind={ActorKind}"
                : string.Empty;
            return $"ActorGroupRearmRequest(Target={Target}, Reason='{Reason ?? "null"}', ActorIds={count}{kindInfo})";
        }
    }

    /// <summary>
    /// Contexto corrente de reset, compartilhado entre etapas e participantes.
    /// </summary>
    public readonly struct ActorGroupRearmContext
    {
        public ActorGroupRearmContext(
            string sceneName,
            ActorGroupRearmRequest request,
            int requestSerial,
            int frameStarted,
            ActorGroupRearmStep currentStep)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            CurrentStep = currentStep;
        }

        public string SceneName { get; }

        public ActorGroupRearmRequest Request { get; }

        public int RequestSerial { get; }

        public int FrameStarted { get; }

        public ActorGroupRearmStep CurrentStep { get; }

        public ActorGroupRearmContext WithStep(ActorGroupRearmStep step)
        {
            return new ActorGroupRearmContext(SceneName, Request, RequestSerial, FrameStarted, step);
        }

        public override string ToString()
        {
            return $"ActorGroupRearmContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Step={CurrentStep}, {Request})";
        }
    }

    /// <summary>
    /// Participante assíncrono de reset de gameplay (recomendado).
    /// </summary>
    public interface IActorGroupRearmable
    {
        Task ResetCleanupAsync(ActorGroupRearmContext ctx);
        Task ResetRestoreAsync(ActorGroupRearmContext ctx);
        Task ResetRebindAsync(ActorGroupRearmContext ctx);
    }

    /// <summary>
    /// Participante síncrono. Um orchestrator pode adaptar para Task.
    /// </summary>
    public interface IActorGroupRearmableSync
    {
        void ResetCleanup(ActorGroupRearmContext ctx);
        void ResetRestore(ActorGroupRearmContext ctx);
        void ResetRebind(ActorGroupRearmContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada etapa. Menor primeiro.
    /// </summary>
    public interface IActorGroupRearmOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos alvos/escopos.
    /// </summary>
    public interface IActorGroupRearmTargetFilter
    {
        bool ShouldParticipate(ActorGroupRearmTarget target);
    }

    public interface IActorGroupRearmOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(ActorGroupRearmRequest request);
    }
}

