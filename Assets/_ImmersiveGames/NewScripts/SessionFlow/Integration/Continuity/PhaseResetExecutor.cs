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
        Task ResetPhaseAsync(PhaseResetContext resetContext, string reason, CancellationToken ct);
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

        public async Task ResetPhaseAsync(PhaseResetContext resetContext, string reason, CancellationToken ct)
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

            await _operationalHandoffService.ExecuteAsync(handoffRequest, ct);

            DebugUtility.Log<PhaseResetExecutor>(
                $"[OBS][PhaseReset] HandoffCompleted target='PhaseResetOperational' scene='{activeScene}' phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' phaseSignature='{resetContext.PhaseSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            EventBus<PhaseResetCompletedEvent>.Raise(
                new PhaseResetCompletedEvent(
                    resetContext,
                    normalizedReason,
                    source: nameof(PhaseResetExecutor)));
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

