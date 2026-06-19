using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.SessionActivity.Simulation
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionActivitySimulationGate
    {

        public ActivityExecutionBlockingState State { get; } = new();

        public ActivityExecutionBlockingResult Execute(ActivityExecutionBlockingCommand command)
        {
            if (command.Kind == ActivityExecutionBlockingCommandKind.Unknown)
            {
                return Reject(command, "unsupported_gate_command", "Unsupported gate command.");
            }

            if (string.IsNullOrWhiteSpace(command.Source) || string.IsNullOrWhiteSpace(command.Reason))
            {
                return Reject(command, "gate_identity_missing", "Gate command source and reason are required.");
            }

            LogCommand(command);

            return command.Kind switch
            {
                ActivityExecutionBlockingCommandKind.BlockActivityExecution => ExecuteBlockActivity(command),
                ActivityExecutionBlockingCommandKind.ReleaseActivityExecution => ExecuteReleaseActivity(command),
                ActivityExecutionBlockingCommandKind.BlockSessionSimulation => ExecuteBlockSession(command),
                ActivityExecutionBlockingCommandKind.ReleaseSessionSimulation => ExecuteReleaseSession(command),
                _ => Reject(command, "unsupported_gate_command", $"Unsupported gate command '{command.Kind}'.")
            };
        }

        private ActivityExecutionBlockingResult ExecuteBlockActivity(ActivityExecutionBlockingCommand command)
        {
            if (!command.Identity.HasActivityScope)
            {
                return Reject(command, "gate_identity_missing", "Activity gate identity is missing.");
            }

            if (State.ActivityBlocked)
            {
                if (State.ActivityIdentity.MatchesActivityScope(command.Identity))
                {
                    return Accept(command, ActivityExecutionBlockingFactKind.ActivityExecutionBlocked, "Activity simulation already blocked. Idempotent no-op applied.");
                }

                return Reject(command, "stale_or_foreign_gate_command", "Activity simulation block belongs to a different identity.");
            }

            State.ActivityBlocked = true;
            State.ActivityIdentity = command.Identity;
            return Accept(command, ActivityExecutionBlockingFactKind.ActivityExecutionBlocked, "Activity simulation blocked.");
        }

        private ActivityExecutionBlockingResult ExecuteReleaseActivity(ActivityExecutionBlockingCommand command)
        {
            if (!command.Identity.HasActivityScope)
            {
                return Reject(command, "gate_identity_missing", "Activity gate identity is missing.");
            }

            if (!State.ActivityBlocked)
            {
                return Accept(command, ActivityExecutionBlockingFactKind.ActivityExecutionReleased, "Activity simulation already released. Idempotent no-op applied.");
            }

            if (!State.ActivityIdentity.MatchesActivityScope(command.Identity))
            {
                return Reject(command, "stale_or_foreign_gate_command", "Activity simulation release belongs to a different identity.");
            }

            State.ActivityBlocked = false;
            State.ActivityIdentity = default;
            return Accept(command, ActivityExecutionBlockingFactKind.ActivityExecutionReleased, "Activity simulation released.");
        }

        private ActivityExecutionBlockingResult ExecuteBlockSession(ActivityExecutionBlockingCommand command)
        {
            if (!command.Identity.HasSessionScope)
            {
                return Reject(command, "gate_identity_missing", "Session gate identity is missing.");
            }

            if (State.SessionBlocked)
            {
                if (State.SessionIdentity.MatchesSessionScope(command.Identity))
                {
                    return Accept(command, ActivityExecutionBlockingFactKind.SessionExecutionBlocked, "Session simulation already blocked. Idempotent no-op applied.");
                }

                return Reject(command, "stale_or_foreign_gate_command", "Session simulation block belongs to a different identity.");
            }

            State.SessionBlocked = true;
            State.SessionIdentity = command.Identity;
            return Accept(command, ActivityExecutionBlockingFactKind.SessionExecutionBlocked, "Session simulation blocked.");
        }

        private ActivityExecutionBlockingResult ExecuteReleaseSession(ActivityExecutionBlockingCommand command)
        {
            if (!command.Identity.HasSessionScope)
            {
                return Reject(command, "gate_identity_missing", "Session gate identity is missing.");
            }

            if (!State.SessionBlocked)
            {
                return Accept(command, ActivityExecutionBlockingFactKind.SessionExecutionReleased, "Session simulation already released. Idempotent no-op applied.");
            }

            if (!State.SessionIdentity.MatchesSessionScope(command.Identity))
            {
                return Reject(command, "stale_or_foreign_gate_command", "Session simulation release belongs to a different identity.");
            }

            State.SessionBlocked = false;
            State.SessionIdentity = default;
            return Accept(command, ActivityExecutionBlockingFactKind.SessionExecutionReleased, "Session simulation released.");
        }

        private ActivityExecutionBlockingResult Accept(
            ActivityExecutionBlockingCommand command,
            ActivityExecutionBlockingFactKind factKind,
            string message)
        {
            SimulationGateFact fact = new(
                factKind,
                command.Identity,
                command.Source,
                command.Reason,
                message);

            var snapshot = BuildSnapshot(command, fact, message);
            UpdateDiagnostics(fact, snapshot);
            LogResult(command, fact, snapshot, true);

            return new ActivityExecutionBlockingResult(command, new[] { fact }, snapshot, "accepted");
        }

        private ActivityExecutionBlockingResult Reject(
            ActivityExecutionBlockingCommand command,
            string rejectionReason,
            string message)
        {
            SimulationGateFact fact = new(
                ActivityExecutionBlockingFactKind.ActivityExecutionBlockingCommandRejected,
                command.Identity,
                command.Source,
                rejectionReason,
                message);

            var snapshot = BuildSnapshot(command, fact, message);
            UpdateDiagnostics(fact, snapshot);
            LogResult(command, fact, snapshot, false);

            return new ActivityExecutionBlockingResult(command, new[] { fact }, snapshot, rejectionReason);
        }

        private SimulationGateSnapshot BuildSnapshot(
            ActivityExecutionBlockingCommand command,
            SimulationGateFact fact,
            string message)
        {
            return new SimulationGateSnapshot(
                command.Kind,
                command.Identity,
                State.SessionBlocked,
                State.SessionIdentity,
                State.ActivityBlocked,
                State.ActivityIdentity,
                fact,
                command.Source,
                command.Reason,
                message);
        }

        private void UpdateDiagnostics(SimulationGateFact fact, SimulationGateSnapshot snapshot)
        {
            State.LastFact = fact;
            State.LastSnapshot = snapshot;
        }

        private static void LogCommand(ActivityExecutionBlockingCommand command)
        {
            DebugUtility.Log(typeof(SessionActivitySimulationGate),
                $"commandKind='{command.Kind}' pipelineId='{command.Identity.PipelineId}' sessionStateId='{command.Identity.SessionStateId}' activityId='{command.Identity.ActivityId}' activityOrdinal='{command.Identity.ActivityOrdinal}' entrySequence='{command.Identity.EntrySequence}' stage='{command.Identity.Stage}' source='{command.Source}' reason='{command.Reason}' decisionSource='pipeline.command'.",
                DebugUtility.Colors.Info);
        }

        private static void LogResult(
            ActivityExecutionBlockingCommand command,
            SimulationGateFact fact,
            SimulationGateSnapshot snapshot,
            bool accepted)
        {
            DebugUtility.Log(typeof(SessionActivitySimulationGate),
                $"commandKind='{command.Kind}' factKind='{fact.Kind}' pipelineId='{command.Identity.PipelineId}' sessionStateId='{command.Identity.SessionStateId}' activityId='{command.Identity.ActivityId}' activityOrdinal='{command.Identity.ActivityOrdinal}' entrySequence='{command.Identity.EntrySequence}' stage='{command.Identity.Stage}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}' source='{command.Source}' reason='{command.Reason}' decisionSource='pipeline.command' outcomeKind='{(accepted ? "accepted" : "rejected")}'.",
                accepted ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            DebugUtility.Log(typeof(SessionActivitySimulationGate),
                $"snapshot commandKind='{snapshot.CommandKind}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}' sessionIdentity='{snapshot.SessionIdentity}' activityIdentity='{snapshot.ActivityIdentity}' lastFactKind='{snapshot.LastFact.Kind}' lastFactReason='{snapshot.LastFact.Reason}' source='{snapshot.Source}' reason='{snapshot.Reason}' message='{snapshot.Message}'.",
                accepted ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }
    }
}
