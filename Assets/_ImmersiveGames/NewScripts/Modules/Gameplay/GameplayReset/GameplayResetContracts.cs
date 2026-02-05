using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.GameplayReset
{
    /// <summary>
    /// Etapas assíncronas de reset para componentes de gameplay.
    /// </summary>
    public enum GameplayResetStep
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }

    /// <summary>
    /// Alvo/escopo suportado para reset de gameplay.
    /// </summary>
    public enum GameplayResetTarget
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
    public readonly struct GameplayResetRequest
    {
        public GameplayResetRequest(
            GameplayResetTarget target,
            string reason = null,
            IReadOnlyList<string> actorIds = null,
            ActorKind actorKind = ActorKind.Unknown)
        {
            Target = target;
            Reason = reason;
            ActorIds = actorIds;
            ActorKind = actorKind;
        }

        public GameplayResetTarget Target { get; }

        public string Reason { get; }

        public IReadOnlyList<string> ActorIds { get; }

        public ActorKind ActorKind { get; }

        public static GameplayResetRequest ByActorKind(ActorKind kind, string reason = null)
        {
            return new GameplayResetRequest(GameplayResetTarget.ByActorKind, reason, actorKind: kind);
        }

        public override string ToString()
        {
            int count = ActorIds?.Count ?? 0;
            string kindInfo = Target == GameplayResetTarget.ByActorKind || Target == GameplayResetTarget.PlayersOnly
                ? $", ActorKind={ActorKind}"
                : string.Empty;
            return $"GameplayResetRequest(Target={Target}, Reason='{Reason ?? "null"}', ActorIds={count}{kindInfo})";
        }
    }

    /// <summary>
    /// Contexto corrente de reset, compartilhado entre etapas e participantes.
    /// </summary>
    public readonly struct GameplayResetContext
    {
        public GameplayResetContext(
            string sceneName,
            GameplayResetRequest request,
            int requestSerial,
            int frameStarted,
            GameplayResetStep currentStep)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            CurrentStep = currentStep;
        }

        public string SceneName { get; }

        public GameplayResetRequest Request { get; }

        public int RequestSerial { get; }

        public int FrameStarted { get; }

        public GameplayResetStep CurrentStep { get; }

        public GameplayResetContext WithStep(GameplayResetStep step)
        {
            return new GameplayResetContext(SceneName, Request, RequestSerial, FrameStarted, step);
        }

        public override string ToString()
        {
            return $"GameplayResetContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Step={CurrentStep}, {Request})";
        }
    }

    /// <summary>
    /// Participante assíncrono de reset de gameplay (recomendado).
    /// </summary>
    public interface IGameplayResettable
    {
        Task ResetCleanupAsync(GameplayResetContext ctx);
        Task ResetRestoreAsync(GameplayResetContext ctx);
        Task ResetRebindAsync(GameplayResetContext ctx);
    }

    /// <summary>
    /// Participante síncrono (fallback). Um orchestrator pode adaptar para Task.
    /// </summary>
    public interface IGameplayResettableSync
    {
        void ResetCleanup(GameplayResetContext ctx);
        void ResetRestore(GameplayResetContext ctx);
        void ResetRebind(GameplayResetContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada etapa. Menor primeiro.
    /// </summary>
    public interface IGameplayResetOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos alvos/escopos.
    /// </summary>
    public interface IGameplayResetTargetFilter
    {
        bool ShouldParticipate(GameplayResetTarget target);
    }

    public interface IGameplayResetOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(GameplayResetRequest request);
    }
}
