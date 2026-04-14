using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionTransitionOrchestrator
    {
        private readonly IGameplaySessionFlowContinuityService _continuityService;

        public SessionTransitionOrchestrator(IGameplaySessionFlowContinuityService continuityService)
        {
            _continuityService = continuityService ?? throw new ArgumentNullException(nameof(continuityService));
        }

        public async Task ExecuteAsync(SessionTransitionPlan plan, CancellationToken ct = default)
        {
            if (!plan.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionOrchestrator),
                    "[FATAL][H1][SessionTransition] SessionTransitionPlan invalido recebido pelo orquestrador.");
            }

            string normalizedReason = Normalize(plan.Reason);

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteStarted continuation='{plan.ResolvedContinuation}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' reason='{normalizedReason}' nextState='{Normalize(plan.NextState)}'.",
                DebugUtility.Colors.Info);

            ct.ThrowIfCancellationRequested();

            if (plan.PhaseAction == SessionTransitionPhaseAction.NextPhase ||
                plan.ResetAction == SessionTransitionResetAction.PhaseReset)
            {
                EventBus<SessionTransitionPhaseLocalEntryReadyEvent>.Raise(
                    new SessionTransitionPhaseLocalEntryReadyEvent(plan, nameof(SessionTransitionOrchestrator)));
            }

            if (plan.ResetAction == SessionTransitionResetAction.PhaseReset)
            {
                await _continuityService.ResetCurrentPhaseAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (plan.PhaseAction == SessionTransitionPhaseAction.NextPhase)
            {
                await _continuityService.NextPhaseAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (plan.HandoffAction == SessionTransitionHandoffAction.GoToMenu)
            {
                await _continuityService.ExitToMenuAsync(normalizedReason, ct);
                DebugUtility.Log<SessionTransitionOrchestrator>(
                    $"[OBS][GameplaySessionFlow][SessionTransition] ExecuteCompleted continuation='{plan.ResolvedContinuation}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' handoffAction='{plan.HandoffAction}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.Log<SessionTransitionOrchestrator>(
                $"[OBS][GameplaySessionFlow][SessionTransition] SkipNoContent continuation='{plan.ResolvedContinuation}' reason='{normalizedReason}' phaseAction='{plan.PhaseAction}' resetAction='{plan.ResetAction}' handoffAction='{plan.HandoffAction}'.",
                DebugUtility.Colors.Warning);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
