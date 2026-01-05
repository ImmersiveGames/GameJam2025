using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Implementação padrão do serviço global de fases.
    /// </summary>
    public sealed class WorldPhaseService : IWorldPhaseService
    {
        private readonly object _sync = new();
        private PhaseId _currentPhaseId;
        private PhaseId? _requestedPhaseId;
        private int _epoch;
        private int? _seed;

        public WorldPhaseService(PhaseId defaultPhaseId)
        {
            _currentPhaseId = defaultPhaseId.IsValid ? defaultPhaseId : new PhaseId("Phase1");
            _epoch = 1;
        }

        public PhaseId CurrentPhaseId
        {
            get
            {
                lock (_sync)
                {
                    return _currentPhaseId;
                }
            }
        }

        public PhaseId? RequestedPhaseId
        {
            get
            {
                lock (_sync)
                {
                    return _requestedPhaseId;
                }
            }
        }

        public int Epoch
        {
            get
            {
                lock (_sync)
                {
                    return _epoch;
                }
            }
        }

        public int? Seed
        {
            get
            {
                lock (_sync)
                {
                    return _seed;
                }
            }
        }

        public PhaseSnapshot CaptureSnapshot(string reason)
        {
            PhaseSnapshot snapshot;
            lock (_sync)
            {
                snapshot = new PhaseSnapshot(_currentPhaseId, _requestedPhaseId, _epoch, _seed);
            }

            DebugUtility.Log(typeof(WorldPhaseService),
                $"[Phase] Snapshot captured phaseId='{snapshot.CurrentPhaseId.Value}' requested='{snapshot.RequestedPhaseLabel}' epoch={snapshot.Epoch} reason='{reason ?? "<null>"}'");

            return snapshot;
        }

        public PhaseSnapshot CommitRequestedPhase(string reason)
        {
            PhaseSnapshot committedSnapshot;
            PhaseId? requested;
            PhaseId from;
            PhaseId to;
            int epoch;

            lock (_sync)
            {
                requested = _requestedPhaseId;
                from = _currentPhaseId;

                if (!requested.HasValue || !requested.Value.IsValid)
                {
                    committedSnapshot = new PhaseSnapshot(_currentPhaseId, _requestedPhaseId, _epoch, _seed);
                    return committedSnapshot;
                }

                to = requested.Value;
                _currentPhaseId = to;
                _requestedPhaseId = null;
                _epoch++;
                epoch = _epoch;

                committedSnapshot = new PhaseSnapshot(_currentPhaseId, _requestedPhaseId, _epoch, _seed);
            }

            DebugUtility.Log(typeof(WorldPhaseService),
                $"[Phase] Phase commit from='{from.Value}' to='{to.Value}' epoch={epoch} reason='{reason ?? "<null>"}'");

            return committedSnapshot;
        }

        public void RequestPhase(PhaseId phaseId, string reason)
        {
            if (!phaseId.IsValid)
            {
                DebugUtility.LogWarning(typeof(WorldPhaseService),
                    $"[Phase] RequestPhase ignored (invalid). reason='{reason ?? "<null>"}'");
                return;
            }

            lock (_sync)
            {
                _requestedPhaseId = phaseId;
            }

            DebugUtility.Log(typeof(WorldPhaseService),
                $"[Phase] RequestPhase phaseId='{phaseId.Value}' reason='{reason ?? "<null>"}'");
        }

        public void RestartPhase(string reason)
        {
            PhaseId phaseId;
            lock (_sync)
            {
                phaseId = _currentPhaseId;
                _requestedPhaseId = phaseId;
            }

            DebugUtility.Log(typeof(WorldPhaseService),
                $"[Phase] RestartPhase phaseId='{phaseId.Value}' reason='{reason ?? "<null>"}'");
        }
    }
}
