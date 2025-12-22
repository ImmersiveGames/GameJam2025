using System.Collections.Generic;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Infrastructure.World.Reset
{
    /// <summary>
    /// Fases assíncronas de reset para componentes de gameplay.
    /// </summary>
    public enum GameplayResetStructs
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }

    /// <summary>
    /// Escopos suportados para reset de gameplay.
    /// </summary>
    public enum GameplayResetScope
    {
        AllActorsInScene = 0,
        PlayersOnly = 1,
        EaterOnly = 2,
        ActorIdSet = 3
    }

    /// <summary>
    /// Pedido de reset contextualizado com escopo e actorIds (quando aplicável).
    /// </summary>
    public readonly struct GameplayResetRequest
    {
        public GameplayResetRequest(GameplayResetScope scope, string reason = null, IReadOnlyList<string> actorIds = null)
        {
            Scope = scope;
            Reason = reason;
            ActorIds = actorIds;
        }

        public GameplayResetScope Scope { get; }

        public string Reason { get; }

        public IReadOnlyList<string> ActorIds { get; }

        public override string ToString()
        {
            int count = ActorIds != null ? ActorIds.Count : 0;
            return $"ResetRequest(Scope={Scope}, Reason='{Reason ?? "null"}', ActorIds={count})";
        }
    }

    /// <summary>
    /// Contexto corrente de reset, compartilhado entre fases e participantes.
    /// </summary>
    public readonly struct GameplayResetContext
    {
        public GameplayResetContext(
            string sceneName,
            GameplayResetRequest request,
            int requestSerial,
            int frameStarted,
            GameplayResetStructs currentStructs)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            CurrentStructs = currentStructs;
        }

        public string SceneName { get; }

        public GameplayResetRequest Request { get; }

        public int RequestSerial { get; }

        public int FrameStarted { get; }

        public GameplayResetStructs CurrentStructs { get; }

        public GameplayResetContext WithPhase(GameplayResetStructs structs)
        {
            return new GameplayResetContext(SceneName, Request, RequestSerial, FrameStarted, structs);
        }

        public override string ToString()
        {
            return $"ResetContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Phase={CurrentStructs}, {Request})";
        }
    }

    /// <summary>
    /// Participante assíncrono de reset (recomendado).
    /// </summary>
    public interface IResetInterfaces
    {
        Task Reset_CleanupAsync(GameplayResetContext ctx);
        Task Reset_RestoreAsync(GameplayResetContext ctx);
        Task Reset_RebindAsync(GameplayResetContext ctx);
    }

    /// <summary>
    /// Participante síncrono (fallback). O orchestrator adapta para Task.
    /// </summary>
    public interface IResetParticipantSync
    {
        void Reset_Cleanup(GameplayResetContext ctx);
        void Reset_Restore(GameplayResetContext ctx);
        void Reset_Rebind(GameplayResetContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada fase.
    /// Menor primeiro.
    /// </summary>
    public interface IResetOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos escopos.
    /// </summary>
    public interface IResetScopeFilter
    {
        bool ShouldParticipate(GameplayResetScope scope);
    }

    public interface IResetOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(GameplayResetRequest request);
    }
}
