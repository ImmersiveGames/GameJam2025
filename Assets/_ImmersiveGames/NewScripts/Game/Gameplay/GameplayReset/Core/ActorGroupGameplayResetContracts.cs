using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Execution;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Policies
{
    /// <summary>
    /// Policy local de GameplayReset.
    /// </summary>
    public interface IActorGroupGameplayResetPolicy
    {
        string Name { get; }

        bool IsStrict { get; }

        bool AllowSceneScan { get; }

        void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null);
    }

    /// <summary>
    /// Policy padrão do GameplayReset.
    /// - Strict = UNITY_EDITOR/DEVELOPMENT_BUILD
    /// - SceneScan: opt-in apenas em Strict (QA/Dev)
    /// </summary>
    public sealed class ProductionActorGroupGameplayResetPolicy : IActorGroupGameplayResetPolicy
    {
        private readonly _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode.IRuntimeModeProvider _runtimeModeProvider;
        private readonly _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode.IDegradedModeReporter _degradedModeReporter;

        public ProductionActorGroupGameplayResetPolicy(
            _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode.IRuntimeModeProvider runtimeModeProvider,
            _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode.IDegradedModeReporter degradedModeReporter)
        {
            _runtimeModeProvider = runtimeModeProvider;
            _degradedModeReporter = degradedModeReporter;
        }

        public string Name => IsStrict ? "Strict" : "Release";

        public bool IsStrict => _runtimeModeProvider != null && _runtimeModeProvider.IsStrict;

        public bool AllowSceneScan => IsStrict;

        public void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null)
        {
            if (_degradedModeReporter == null)
            {
                return;
            }

            _degradedModeReporter.Report(feature, reason, detail, signature, profile);
        }
    }
}

namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Observability
{
    /// <summary>
    /// Identificadores canônicos de observabilidade para GameplayReset.
    /// </summary>
    public static class GameplayResetFeatureIds
    {
        public const string GameplayReset = "gameplay.reset";
    }
}

namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Core
{
    /// <summary>
    /// Etapas assíncronas de reset para componentes de gameplay.
    /// </summary>
    public enum ActorGroupGameplayResetStep
    {
        Cleanup = 0,
        Restore = 1,
        Rebind = 2
    }

    /// <summary>
    /// Alvos canônicos suportados para reset de gameplay.
    /// </summary>
    public enum ActorGroupGameplayResetTarget
    {
        ByActorKind = 0,
        ActorIdSet = 1
    }

    /// <summary>
    /// Pedido de reset contextualizado com alvo e actorIds (quando aplicável).
    /// </summary>
    public readonly struct ActorGroupGameplayResetRequest
    {
        public ActorGroupGameplayResetRequest(
            ActorGroupGameplayResetTarget target,
            string reason = null,
            IReadOnlyList<string> actorIds = null,
            ActorKind actorKind = ActorKind.Unknown)
        {
            Target = target;
            Reason = reason;
            ActorIds = actorIds;
            ActorKind = actorKind;
        }

        public ActorGroupGameplayResetTarget Target { get; }

        public string Reason { get; }

        public IReadOnlyList<string> ActorIds { get; }

        public ActorKind ActorKind { get; }

        public static ActorGroupGameplayResetRequest ByActorKind(ActorKind kind, string reason = null)
        {
            return new ActorGroupGameplayResetRequest(ActorGroupGameplayResetTarget.ByActorKind, reason, actorKind: kind);
        }

        public static ActorGroupGameplayResetRequest ForActorIds(IReadOnlyList<string> actorIds, string reason = null)
        {
            return new ActorGroupGameplayResetRequest(ActorGroupGameplayResetTarget.ActorIdSet, reason, actorIds: actorIds);
        }

        public override string ToString()
        {
            int count = ActorIds?.Count ?? 0;
            string kindInfo = Target == ActorGroupGameplayResetTarget.ByActorKind
                ? $", ActorKind={ActorKind}"
                : string.Empty;
            return $"ActorGroupGameplayResetRequest(Target={Target}, Reason='{Reason ?? "null"}', ActorIds={count}{kindInfo})";
        }
    }

    /// <summary>
    /// Contexto corrente de reset, compartilhado entre etapas e participantes.
    /// </summary>
    public readonly struct ActorGroupGameplayResetContext
    {
        public ActorGroupGameplayResetContext(
            string sceneName,
            ActorGroupGameplayResetRequest request,
            int requestSerial,
            int frameStarted,
            ActorGroupGameplayResetStep currentStep)
        {
            SceneName = sceneName;
            Request = request;
            RequestSerial = requestSerial;
            FrameStarted = frameStarted;
            CurrentStep = currentStep;
        }

        public string SceneName { get; }

        public ActorGroupGameplayResetRequest Request { get; }

        public int RequestSerial { get; }

        public int FrameStarted { get; }

        public ActorGroupGameplayResetStep CurrentStep { get; }

        public ActorGroupGameplayResetContext WithStep(ActorGroupGameplayResetStep step)
        {
            return new ActorGroupGameplayResetContext(SceneName, Request, RequestSerial, FrameStarted, step);
        }

        public override string ToString()
        {
            return $"ActorGroupGameplayResetContext(Scene='{SceneName}', Serial={RequestSerial}, Frame={FrameStarted}, Step={CurrentStep}, {Request})";
        }
    }

    /// <summary>
    /// Participante assíncrono de gameplay reset sobre um actor ja vivo,
    /// materializado e registrado pelo trilho de Spawn.
    /// </summary>
    public interface IActorGroupGameplayResettable
    {
        Task ResetCleanupAsync(ActorGroupGameplayResetContext ctx);
        Task ResetRestoreAsync(ActorGroupGameplayResetContext ctx);
        Task ResetRebindAsync(ActorGroupGameplayResetContext ctx);
    }

    /// <summary>
    /// Participante síncrono sobre um actor ja vivo, materializado e registrado.
    /// Um orchestrator pode adaptar para Task.
    /// </summary>
    public interface IActorGroupGameplayResettableSync
    {
        void ResetCleanup(ActorGroupGameplayResetContext ctx);
        void ResetRestore(ActorGroupGameplayResetContext ctx);
        void ResetRebind(ActorGroupGameplayResetContext ctx);
    }

    /// <summary>
    /// Opcional: controla a ordem de execução dentro de cada etapa. Menor primeiro.
    /// </summary>
    public interface IActorGroupGameplayResetOrder
    {
        int ResetOrder { get; }
    }

    /// <summary>
    /// Opcional: permite que o participante ignore certos alvos/escopos.
    /// </summary>
    public interface IActorGroupGameplayResetTargetFilter
    {
        bool ShouldParticipate(ActorGroupGameplayResetTarget target);
    }

    internal interface IActorGroupGameplayResetTargetResolver
    {
        IReadOnlyList<ResetTarget> ResolveTargets(
            ActorGroupGameplayResetRequest request,
            out bool usedSceneScan,
            out bool scanDisabled);
    }

    internal interface IActorGroupGameplayResetExecutor
    {
        Task ExecuteAsync(ResetTarget target, ActorGroupGameplayResetRequest request, int serial);
    }

    public interface IActorGroupGameplayResetOrchestrator
    {
        bool IsResetInProgress { get; }

        /// <summary>
        /// Solicita reset e aguarda conclusão.
        /// Se já houver reset em andamento, a implementação pode ignorar (retornando false) ou aguardar.
        /// </summary>
        Task<bool> RequestResetAsync(ActorGroupGameplayResetRequest request);
    }

    internal interface IActorGroupGameplayResetPolicyAware
    {
        _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Policies.IActorGroupGameplayResetPolicy Policy { get; }
    }
}


