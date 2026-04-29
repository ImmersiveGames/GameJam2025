using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    public interface IPhaseResetExecutor
    {
        Task<PhaseResetExecutionResult> ResetPhaseAsync(PhaseResetContext resetContext, string reason, CancellationToken ct);
    }

    public enum PhaseResetExecutionStatus
    {
        Completed = 0,
        Rejected = 1,
        Unconfirmed = 2,
        Failed = 3
    }

    public readonly struct PhaseResetExecutionResult
    {
        private PhaseResetExecutionResult(
            PhaseResetExecutionStatus status,
            PhaseResetContext resetContext,
            string sceneName,
            string reason,
            string source,
            string detail)
        {
            Status = status;
            ResetContext = resetContext;
            SceneName = Normalize(sceneName);
            Reason = Normalize(reason);
            Source = Normalize(source);
            Detail = Normalize(detail);
        }

        public PhaseResetExecutionStatus Status { get; }
        public bool Succeeded => Status == PhaseResetExecutionStatus.Completed;
        public bool AllowsPhaseLocalEntryReady => Succeeded;
        public PhaseResetContext ResetContext { get; }
        public string SceneName { get; }
        public string Reason { get; }
        public string Source { get; }
        public string Detail { get; }

        public static PhaseResetExecutionResult Completed(
            PhaseResetContext resetContext,
            string sceneName,
            string reason,
            string source)
        {
            return new PhaseResetExecutionResult(
                PhaseResetExecutionStatus.Completed,
                resetContext,
                sceneName,
                reason,
                source,
                "Phase reset operational handoff completed and PhaseResetCompletedEvent was raised.");
        }

        public static PhaseResetExecutionResult Rejected(
            PhaseResetContext resetContext,
            string sceneName,
            string reason,
            string source,
            string detail)
        {
            return new PhaseResetExecutionResult(
                PhaseResetExecutionStatus.Rejected,
                resetContext,
                sceneName,
                reason,
                source,
                detail);
        }

        public static PhaseResetExecutionResult Unconfirmed(
            PhaseResetContext resetContext,
            string sceneName,
            string reason,
            string source,
            string detail)
        {
            return new PhaseResetExecutionResult(
                PhaseResetExecutionStatus.Unconfirmed,
                resetContext,
                sceneName,
                reason,
                source,
                detail);
        }

        public static PhaseResetExecutionResult Failed(
            PhaseResetContext resetContext,
            string sceneName,
            string reason,
            string source,
            string detail)
        {
            return new PhaseResetExecutionResult(
                PhaseResetExecutionStatus.Failed,
                resetContext,
                sceneName,
                reason,
                source,
                detail);
        }

        public override string ToString()
        {
            return $"Status='{Status}', Succeeded='{Succeeded}', AllowsPhaseLocalEntryReady='{AllowsPhaseLocalEntryReady}', SceneName='{SceneName}', Reason='{Reason}', Source='{Source}', Detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseResetExecutor : IPhaseResetExecutor
    {
        private readonly IRestartContextService _restartContextService;
        private readonly IPhaseResetOperationalHandoffService _operationalHandoffService;

        public PhaseResetExecutor(
            IRestartContextService restartContextService,
            IPhaseResetOperationalHandoffService operationalHandoffService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
            _operationalHandoffService = operationalHandoffService ?? throw new ArgumentNullException(nameof(operationalHandoffService));
        }

        public async Task<PhaseResetExecutionResult> ResetPhaseAsync(PhaseResetContext resetContext, string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!resetContext.IsValid)
            {
                FailFastConfig($"ResetPhaseAsync received invalid phase reset context. reason='{reason ?? "<null>"}'.");
            }

            if (resetContext.PhaseDefinitionRef == null)
            {
                FailFastConfig($"ResetPhaseAsync received null phaseDefinitionRef. routeId='{resetContext.MacroRouteId}', reason='{reason ?? "<null>"}'.");
            }

            if (!resetContext.PhaseSignature.IsValid)
            {
                FailFastConfig($"ResetPhaseAsync received empty phaseSignature. phaseRef='{resetContext.PhaseDefinitionRef.name}', reason='{reason ?? "<null>"}'.");
            }

            string normalizedReason = NormalizeReason(reason, "PhaseReset/Level");
            if (!_restartContextService.TryGetCurrent(out GameplayStartSnapshot snapshot) || !snapshot.IsValid || !snapshot.HasPhaseDefinitionRef)
            {
                FailFastConfig($"ResetPhaseAsync without valid gameplay phase snapshot. phaseRef='{resetContext.PhaseDefinitionRef.name}', reason='{normalizedReason}'.");
            }

            if (!ReferenceEquals(snapshot.PhaseDefinitionRef, resetContext.PhaseDefinitionRef))
            {
                FailFastConfig($"ResetPhaseAsync phaseDefinitionRef mismatch. expected='{snapshot.PhaseDefinitionRef.name}', got='{resetContext.PhaseDefinitionRef.name}', reason='{normalizedReason}'.");
            }

            if (snapshot.MacroRouteId != resetContext.MacroRouteId)
            {
                FailFastConfig($"ResetPhaseAsync macroRouteId mismatch. expected='{snapshot.MacroRouteId}', got='{resetContext.MacroRouteId}', reason='{normalizedReason}'.");
            }

            DebugUtility.Log<PhaseResetExecutor>(
                $"[OBS][PhaseReset] ResetPhase phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' phaseSignature='{resetContext.PhaseSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            string activeScene = SceneManager.GetActiveScene().name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(activeScene))
            {
                FailFastConfig($"ResetPhaseAsync could not resolve active scene name. reason='{normalizedReason}'.");
            }

            var handoffRequest = new PhaseResetHandoffRequest(
                resetContext,
                activeScene,
                normalizedReason,
                nameof(PhaseResetExecutor));
            if (!handoffRequest.IsValid)
            {
                FailFastConfig($"ResetPhaseAsync produced invalid handoff request. activeScene='{activeScene}' reason='{normalizedReason}'.");
            }

            DebugUtility.Log<PhaseResetExecutor>(
                $"[OBS][PhaseReset] HandoffDispatch target='PhaseResetOperational' scene='{activeScene}' phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' phaseSignature='{resetContext.PhaseSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            PhaseResetOperationalHandoffResult handoffResult = await _operationalHandoffService.ExecuteAsync(handoffRequest, ct);

            DebugUtility.Log<PhaseResetExecutor>(
                $"[OBS][PhaseReset] HandoffCompleted target='PhaseResetOperational' scene='{activeScene}' phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' phaseSignature='{resetContext.PhaseSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}' status='{handoffResult.Status}' allowsPhaseLocalEntryReady='{handoffResult.AllowsPhaseLocalEntryReady}' detail='{handoffResult.Detail}'.",
                handoffResult.Succeeded ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (!handoffResult.Succeeded || !handoffResult.AllowsPhaseLocalEntryReady)
            {
                return MapHandoffFailure(resetContext, activeScene, normalizedReason, handoffResult);
            }

            EventBus<PhaseResetCompletedEvent>.Raise(
                new PhaseResetCompletedEvent(
                    resetContext,
                    normalizedReason,
                    source: nameof(PhaseResetExecutor)));

            return PhaseResetExecutionResult.Completed(
                resetContext,
                activeScene,
                normalizedReason,
                nameof(PhaseResetExecutor));
        }

        private static PhaseResetExecutionResult MapHandoffFailure(
            PhaseResetContext resetContext,
            string activeScene,
            string normalizedReason,
            PhaseResetOperationalHandoffResult handoffResult)
        {
            string detail = $"Phase reset operational handoff did not complete. handoffResult='{handoffResult}'.";
            return handoffResult.Status switch
            {
                PhaseResetOperationalHandoffStatus.Failed => PhaseResetExecutionResult.Failed(resetContext, activeScene, normalizedReason, nameof(PhaseResetExecutor), detail),
                PhaseResetOperationalHandoffStatus.Rejected => PhaseResetExecutionResult.Rejected(resetContext, activeScene, normalizedReason, nameof(PhaseResetExecutor), detail),
                _ => PhaseResetExecutionResult.Unconfirmed(resetContext, activeScene, normalizedReason, nameof(PhaseResetExecutor), detail),
            };
        }

        private static string NormalizeReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private static void FailFastConfig(string detail)
        {
            HardFailFastH1.Trigger(typeof(PhaseResetExecutor), $"[FATAL][H1][PhaseReset] {detail}");
        }
    }
}
