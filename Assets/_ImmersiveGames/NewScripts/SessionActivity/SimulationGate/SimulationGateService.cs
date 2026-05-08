using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.SimulationGate
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SimulationGateService
    {
        private readonly SimulationGateState _state = new();

        public SimulationGateState State => _state;

        public SimulationGateResult Execute(SimulationGateCommand command)
        {
            if (command.Kind == SimulationGateCommandKind.Unknown)
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
                SimulationGateCommandKind.BlockActivitySimulation => ExecuteBlockActivity(command),
                SimulationGateCommandKind.ReleaseActivitySimulation => ExecuteReleaseActivity(command),
                SimulationGateCommandKind.BlockSessionSimulation => ExecuteBlockSession(command),
                SimulationGateCommandKind.ReleaseSessionSimulation => ExecuteReleaseSession(command),
                _ => Reject(command, "unsupported_gate_command", $"Unsupported gate command '{command.Kind}'."),
            };
        }

        private SimulationGateResult ExecuteBlockActivity(SimulationGateCommand command)
        {
            if (!command.Identity.HasActivityScope)
            {
                return Reject(command, "gate_identity_missing", "Activity gate identity is missing.");
            }

            if (_state.ActivityBlocked)
            {
                if (_state.ActivityIdentity.MatchesActivityScope(command.Identity))
                {
                    return Reject(command, "gate_already_blocked", "Activity simulation is already blocked.");
                }

                return Reject(command, "stale_or_foreign_gate_command", "Activity simulation block belongs to a different identity.");
            }

            _state.ActivityBlocked = true;
            _state.ActivityIdentity = command.Identity;
            return Accept(command, SimulationGateFactKind.ActivitySimulationBlocked, "Activity simulation blocked.");
        }

        private SimulationGateResult ExecuteReleaseActivity(SimulationGateCommand command)
        {
            if (!command.Identity.HasActivityScope)
            {
                return Reject(command, "gate_identity_missing", "Activity gate identity is missing.");
            }

            if (!_state.ActivityBlocked)
            {
                return Reject(command, "gate_not_blocked", "Activity simulation is not blocked.");
            }

            if (!_state.ActivityIdentity.MatchesActivityScope(command.Identity))
            {
                return Reject(command, "stale_or_foreign_gate_command", "Activity simulation release belongs to a different identity.");
            }

            _state.ActivityBlocked = false;
            _state.ActivityIdentity = default;
            return Accept(command, SimulationGateFactKind.ActivitySimulationReleased, "Activity simulation released.");
        }

        private SimulationGateResult ExecuteBlockSession(SimulationGateCommand command)
        {
            if (!command.Identity.HasSessionScope)
            {
                return Reject(command, "gate_identity_missing", "Session gate identity is missing.");
            }

            if (_state.SessionBlocked)
            {
                if (_state.SessionIdentity.MatchesSessionScope(command.Identity))
                {
                    return Reject(command, "gate_already_blocked", "Session simulation is already blocked.");
                }

                return Reject(command, "stale_or_foreign_gate_command", "Session simulation block belongs to a different identity.");
            }

            _state.SessionBlocked = true;
            _state.SessionIdentity = command.Identity;
            return Accept(command, SimulationGateFactKind.SessionSimulationBlocked, "Session simulation blocked.");
        }

        private SimulationGateResult ExecuteReleaseSession(SimulationGateCommand command)
        {
            if (!command.Identity.HasSessionScope)
            {
                return Reject(command, "gate_identity_missing", "Session gate identity is missing.");
            }

            if (!_state.SessionBlocked)
            {
                return Reject(command, "gate_not_blocked", "Session simulation is not blocked.");
            }

            if (!_state.SessionIdentity.MatchesSessionScope(command.Identity))
            {
                return Reject(command, "stale_or_foreign_gate_command", "Session simulation release belongs to a different identity.");
            }

            _state.SessionBlocked = false;
            _state.SessionIdentity = default;
            return Accept(command, SimulationGateFactKind.SessionSimulationReleased, "Session simulation released.");
        }

        private SimulationGateResult Accept(
            SimulationGateCommand command,
            SimulationGateFactKind factKind,
            string message)
        {
            SimulationGateFact fact = new(
                factKind,
                command.Identity,
                command.Source,
                command.Reason,
                message);

            SimulationGateSnapshot snapshot = BuildSnapshot(command, fact, message);
            UpdateDiagnostics(fact, snapshot);
            LogResult(command, fact, snapshot, accepted: true);

            return new SimulationGateResult(command, new[] { fact }, snapshot, "accepted");
        }

        private SimulationGateResult Reject(
            SimulationGateCommand command,
            string rejectionReason,
            string message)
        {
            SimulationGateFact fact = new(
                SimulationGateFactKind.SimulationGateCommandRejected,
                command.Identity,
                command.Source,
                rejectionReason,
                message);

            SimulationGateSnapshot snapshot = BuildSnapshot(command, fact, message);
            UpdateDiagnostics(fact, snapshot);
            LogResult(command, fact, snapshot, accepted: false);

            return new SimulationGateResult(command, new[] { fact }, snapshot, rejectionReason);
        }

        private SimulationGateSnapshot BuildSnapshot(
            SimulationGateCommand command,
            SimulationGateFact fact,
            string message)
        {
            return new SimulationGateSnapshot(
                command.Kind,
                command.Identity,
                _state.SessionBlocked,
                _state.SessionIdentity,
                _state.ActivityBlocked,
                _state.ActivityIdentity,
                fact,
                command.Source,
                command.Reason,
                message);
        }

        private void UpdateDiagnostics(SimulationGateFact fact, SimulationGateSnapshot snapshot)
        {
            _state.LastFact = fact;
            _state.LastSnapshot = snapshot;
        }

        private static void LogCommand(SimulationGateCommand command)
        {
            DebugUtility.Log(typeof(SimulationGateService),
                $"[OBS][SimulationGate] commandKind='{command.Kind}' pipelineId='{command.Identity.PipelineId}' sessionStateId='{command.Identity.SessionStateId}' activityId='{command.Identity.ActivityId}' activityOrdinal='{command.Identity.ActivityOrdinal}' entrySequence='{command.Identity.EntrySequence}' stage='{command.Identity.Stage}' source='{command.Source}' reason='{command.Reason}' decisionSource='pipeline.command'.",
                DebugUtility.Colors.Info);
        }

        private static void LogResult(
            SimulationGateCommand command,
            SimulationGateFact fact,
            SimulationGateSnapshot snapshot,
            bool accepted)
        {
            DebugUtility.Log(typeof(SimulationGateService),
                $"[OBS][SimulationGate] commandKind='{command.Kind}' factKind='{fact.Kind}' pipelineId='{command.Identity.PipelineId}' sessionStateId='{command.Identity.SessionStateId}' activityId='{command.Identity.ActivityId}' activityOrdinal='{command.Identity.ActivityOrdinal}' entrySequence='{command.Identity.EntrySequence}' stage='{command.Identity.Stage}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}' source='{command.Source}' reason='{command.Reason}' decisionSource='pipeline.command' outcome='{(accepted ? "accepted" : "rejected")}'.",
                accepted ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            DebugUtility.Log(typeof(SimulationGateService),
                $"[OBS][SimulationGate] snapshot commandKind='{snapshot.CommandKind}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}' sessionIdentity='{snapshot.SessionIdentity}' activityIdentity='{snapshot.ActivityIdentity}' lastFactKind='{snapshot.LastFact.Kind}' lastFactReason='{snapshot.LastFact.Reason}' source='{snapshot.Source}' reason='{snapshot.Reason}' message='{snapshot.Message}'.",
                accepted ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }
    }
}

