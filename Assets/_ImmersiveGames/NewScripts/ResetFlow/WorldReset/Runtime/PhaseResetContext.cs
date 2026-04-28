using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using System.Threading;
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    public readonly struct PhaseResetContext
    {
        public PhaseResetContext(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            PhaseContextSignature phaseSignature,
            string resetSignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            PhaseSignature = phaseSignature;
            ResetSignature = string.IsNullOrWhiteSpace(resetSignature) ? string.Empty : resetSignature.Trim();
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public PhaseContextSignature PhaseSignature { get; }
        public string ResetSignature { get; }
        public bool IsValid => PhaseDefinitionRef != null && MacroRouteId.IsValid && PhaseSignature.IsValid;
    }

    public readonly struct PhaseResetHandoffRequest
    {
        public PhaseResetHandoffRequest(
            PhaseResetContext resetContext,
            string activeScene,
            string reason,
            string source)
        {
            ResetContext = resetContext;
            ActiveScene = string.IsNullOrWhiteSpace(activeScene) ? string.Empty : activeScene.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseResetContext ResetContext { get; }
        public string ActiveScene { get; }
        public string Reason { get; }
        public string Source { get; }
        public bool IsValid => ResetContext.IsValid && !string.IsNullOrWhiteSpace(ActiveScene) && !string.IsNullOrWhiteSpace(Reason);
    }

    public enum PhaseResetOperationalHandoffStatus
    {
        Completed = 0,
        Rejected = 1,
        Unconfirmed = 2,
        Failed = 3
    }

    public readonly struct PhaseResetOperationalHandoffResult
    {
        private PhaseResetOperationalHandoffResult(
            PhaseResetOperationalHandoffStatus status,
            PhaseResetHandoffRequest request,
            int executorCount,
            string detail)
        {
            Status = status;
            Request = request;
            ExecutorCount = executorCount < 0 ? 0 : executorCount;
            Detail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        }

        public PhaseResetOperationalHandoffStatus Status { get; }
        public PhaseResetHandoffRequest Request { get; }
        public int ExecutorCount { get; }
        public string Detail { get; }
        public bool Succeeded => Status == PhaseResetOperationalHandoffStatus.Completed;
        public bool AllowsPhaseLocalEntryReady => Succeeded;

        public static PhaseResetOperationalHandoffResult Completed(PhaseResetHandoffRequest request, int executorCount, string detail = null)
        {
            return new PhaseResetOperationalHandoffResult(
                PhaseResetOperationalHandoffStatus.Completed,
                request,
                executorCount,
                string.IsNullOrWhiteSpace(detail) ? "Phase reset operational handoff completed." : detail);
        }

        public static PhaseResetOperationalHandoffResult Rejected(PhaseResetHandoffRequest request, int executorCount, string detail)
        {
            return new PhaseResetOperationalHandoffResult(
                PhaseResetOperationalHandoffStatus.Rejected,
                request,
                executorCount,
                detail);
        }

        public static PhaseResetOperationalHandoffResult Unconfirmed(PhaseResetHandoffRequest request, int executorCount, string detail)
        {
            return new PhaseResetOperationalHandoffResult(
                PhaseResetOperationalHandoffStatus.Unconfirmed,
                request,
                executorCount,
                detail);
        }

        public static PhaseResetOperationalHandoffResult Failed(PhaseResetHandoffRequest request, int executorCount, string detail)
        {
            return new PhaseResetOperationalHandoffResult(
                PhaseResetOperationalHandoffStatus.Failed,
                request,
                executorCount,
                detail);
        }

        public override string ToString()
        {
            return $"Status='{Status}', Succeeded='{Succeeded}', AllowsPhaseLocalEntryReady='{AllowsPhaseLocalEntryReady}', Scene='{Request.ActiveScene}', Reason='{Request.Reason}', Source='{Request.Source}', ExecutorCount='{ExecutorCount}', Detail='{Detail}'";
        }
    }

    public interface IPhaseResetOperationalHandoffService
    {
        Task<PhaseResetOperationalHandoffResult> ExecuteAsync(PhaseResetHandoffRequest request, CancellationToken ct);
    }

    public readonly struct PhaseResetCompletedEvent : IEvent
    {
        public PhaseResetCompletedEvent(PhaseResetContext resetContext, string reason, string source)
        {
            ResetContext = resetContext;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public PhaseResetContext ResetContext { get; }
        public string Reason { get; }
        public string Source { get; }

        public bool IsValid => ResetContext.IsValid;
    }
}
