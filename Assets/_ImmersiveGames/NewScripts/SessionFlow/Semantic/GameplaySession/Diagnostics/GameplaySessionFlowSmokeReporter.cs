using System;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;

namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    public static class GameplaySessionFlowSmokeReporter
    {
        public static bool ReportCurrentState(string stage, string reason = null)
        {
            string normalizedStage = Normalize(stage);
            string normalizedReason = Normalize(reason);

            bool hasSession = TryResolveSessionContext(out var sessionContext);
            bool hasPhase = TryResolvePhaseRuntime(out var phaseRuntime);
            bool hasParticipation = TryResolveParticipation(out var participation);

            string sessionState = hasSession ? DescribeSessionContext(sessionContext) : "missing";
            string phaseState = hasPhase ? DescribePhaseRuntime(phaseRuntime) : "missing";
            string participationState = hasParticipation ? DescribeParticipation(participation) : "missing";
            string relation = DescribeSessionPhaseRelation(hasSession, hasPhase, sessionContext, phaseRuntime);
            string participationRelation = DescribeParticipationRelation(hasPhase, hasParticipation, phaseRuntime, participation);

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] stage='{normalizedStage}' reason='{normalizedReason}' sessionContext='{sessionState}' phaseRuntime='{phaseState}' participation='{participationState}' relation='{relation}' participationRelation='{participationRelation}'.",
                DebugUtility.Colors.Info);

            return hasSession || hasPhase || hasParticipation;
        }

        public static void ClearCurrentState(string reason = null)
        {
            string normalizedReason = Normalize(reason);

            if (TryResolveSessionContextService(out var sessionService))
            {
                sessionService.Clear(normalizedReason);
            }

            if (TryResolvePhaseRuntimeService(out var phaseService))
            {
                phaseService.Clear(normalizedReason);
            }

            if (TryResolveParticipationService(out var participationService))
            {
                participationService.Clear(normalizedReason);
            }

            DebugUtility.Log(typeof(GameplaySessionFlowSmokeReporter),
                $"[OBS][GameplaySessionFlow][Smoke] smokeStateCleared reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolveSessionContext(out GameplaySessionContextSnapshot snapshot)
        {
            snapshot = GameplaySessionContextSnapshot.Empty;
            return TryResolveSessionContextService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolvePhaseRuntime(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            snapshot = GameplayPhaseRuntimeSnapshot.Empty;
            return TryResolvePhaseRuntimeService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolveParticipation(out ParticipationSnapshot snapshot)
        {
            snapshot = ParticipationSnapshot.Empty;
            return TryResolveParticipationService(out var service) && service.TryGetCurrent(out snapshot);
        }

        private static bool TryResolveSessionContextService(out IGameplaySessionContextService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolvePhaseRuntimeService(out IGameplayPhaseRuntimeService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static bool TryResolveParticipationService(out IGameplayParticipationFlowService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static string DescribeSessionContext(GameplaySessionContextSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.SessionSignature}' phaseId='{snapshot.PhaseId}' routeId='{snapshot.MacroRouteId}' v='{snapshot.SelectionVersion}'"
                : "empty";
        }

        private static string DescribePhaseRuntime(GameplayPhaseRuntimeSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.PhaseRuntimeSignature}' sessionSignature='{snapshot.SessionContext.SessionSignature}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}' contentCount='{snapshot.ContentEntryCount}' playerCount='{snapshot.PlayerEntryCount}'"
                : "empty";
        }

        private static string DescribeParticipation(ParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled participationSignature='{snapshot.Signature}' phaseSignature='{snapshot.PhaseSignature}' readiness='{snapshot.Readiness.State}' count='{snapshot.ParticipantCount}' primaryId='{snapshot.PrimaryParticipantId}' localId='{snapshot.LocalParticipantId}'"
                : "empty";
        }

        private static string DescribeSessionPhaseRelation(
            bool hasSession,
            bool hasPhase,
            GameplaySessionContextSnapshot sessionContext,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (!hasSession && !hasPhase)
            {
                return "both_empty";
            }

            if (hasSession && !hasPhase)
            {
                return "session_filled_phase_empty";
            }

            if (!hasSession && hasPhase)
            {
                return "phase_without_session";
            }

            return string.Equals(sessionContext.SessionSignature, phaseRuntime.SessionContext.SessionSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string DescribeParticipationRelation(
            bool hasPhase,
            bool hasParticipation,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            ParticipationSnapshot participation)
        {
            if (!hasPhase && !hasParticipation)
            {
                return "both_empty";
            }

            if (hasPhase && !hasParticipation)
            {
                return "phase_filled_participation_empty";
            }

            if (!hasPhase && hasParticipation)
            {
                return "participation_without_phase";
            }

            return string.Equals(phaseRuntime.PhaseRuntimeSignature, participation.PhaseSignature, StringComparison.Ordinal)
                ? "linked"
                : "mismatch";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }
}

