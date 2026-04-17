using System;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;

namespace ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Runtime
{
    public readonly struct SessionIntegrationContextSnapshot
    {
        public SessionIntegrationContextSnapshot(
            GameplaySessionContextSnapshot sessionContext,
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            ParticipationSnapshot participation)
        {
            SessionContext = sessionContext;
            PhaseRuntime = phaseRuntime;
            Participation = participation;
        }

        public GameplaySessionContextSnapshot SessionContext { get; }
        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public ParticipationSnapshot Participation { get; }

        public bool HasSessionContext => SessionContext.IsValid;
        public bool HasPhaseRuntime => PhaseRuntime.IsValid;
        public bool HasParticipation => Participation.IsValid;
        public bool HasCoreContext => HasSessionContext && HasPhaseRuntime;
        public bool HasAnyContext => HasSessionContext || HasPhaseRuntime || HasParticipation;
        public bool IsValid => HasCoreContext;

        public static SessionIntegrationContextSnapshot Empty => new(
            GameplaySessionContextSnapshot.Empty,
            GameplayPhaseRuntimeSnapshot.Empty,
            ParticipationSnapshot.Empty);

        public override string ToString()
        {
            return $"sessionContext='{DescribeSessionContext(SessionContext)}', phaseRuntime='{DescribePhaseRuntime(PhaseRuntime)}', participation='{DescribeParticipation(Participation)}'";
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
                ? $"filled signature='{snapshot.PhaseRuntimeSignature}' phaseRef='{(snapshot.PhaseDefinitionRef != null ? snapshot.PhaseDefinitionRef.name : "<none>")}'"
                : "empty";
        }

        private static string DescribeParticipation(ParticipationSnapshot snapshot)
        {
            return snapshot.IsValid
                ? $"filled signature='{snapshot.Signature}' readiness='{snapshot.Readiness.State}' count='{snapshot.ParticipantCount}'"
                : "empty";
        }
    }
}


