using System;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Application;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    public interface IPhaseResetExecutor
    {
        Task ResetPhaseAsync(PhaseResetContext resetContext, string reason, CancellationToken ct);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseResetExecutor : IPhaseResetExecutor
    {
        private readonly IRestartContextService _restartContextService;
        private readonly WorldResetExecutor _localExecutor = new();

        public PhaseResetExecutor(IRestartContextService restartContextService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
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

            if (!_localExecutor.TryResolveExecutors(activeScene, out var executors) || executors.Count == 0)
            {
                FailFastConfig($"ResetPhaseAsync found no local reset executor for scene='{activeScene}'. reason='{normalizedReason}'.");
            }

            await _localExecutor.ExecuteAsync(executors, normalizedReason);

            DebugUtility.Log<PhaseResetExecutor>(
                $"[OBS][PhaseReset] ResetPhaseCompleted phaseRef='{resetContext.PhaseDefinitionRef.name}' routeId='{resetContext.MacroRouteId}' phaseSignature='{resetContext.PhaseSignature}' resetSignature='{resetContext.ResetSignature}' reason='{normalizedReason}'.",
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
