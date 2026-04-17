using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Runtime
{
    public enum GameplayPhasePlayerParticipationMode
    {
        None = 0,
        SoloCanonical = 1,
        ConfiguredRoster = 2
    }

    public readonly struct GameplayPhasePlayerParticipationSnapshot
    {
        public static GameplayPhasePlayerParticipationSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return FromPhaseDefinitionSelectedEvent(evt, GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        internal static GameplayPhasePlayerParticipationSnapshot FromPhaseDefinitionSelectedEvent(
            PhaseDefinitionSelectedEvent evt,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to build the players snapshot.");
            }

            PhaseDefinitionAsset.PhasePlayersBlock playersBlock = evt.PhaseDefinitionRef.Players;
            if (playersBlock == null || playersBlock.entries == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseDefinition phaseId='{evt.PhaseId}' has no valid players block.");
            }

            int participantCount = 0;
            string primaryParticipantId = string.Empty;
            string primaryParticipantLabel = string.Empty;
            GameplayPhasePlayerParticipationMode participationMode = GameplayPhasePlayerParticipationMode.ConfiguredRoster;

            for (int i = 0; i < playersBlock.entries.Count; i++)
            {
                PhaseDefinitionAsset.PhasePlayerEntry entry = playersBlock.entries[i];
                if (entry == null)
                {
                    continue;
                }

                participantCount++;
                if (string.IsNullOrWhiteSpace(primaryParticipantId))
                {
                    primaryParticipantId = string.IsNullOrWhiteSpace(entry.localId) ? $"player_{i + 1}" : entry.localId.Trim();
                    primaryParticipantLabel = entry.role.ToString();
                }

                if (entry.role == PhaseDefinitionAsset.PhasePlayerRole.Local)
                {
                    primaryParticipantId = string.IsNullOrWhiteSpace(entry.localId) ? $"player_{i + 1}" : entry.localId.Trim();
                    primaryParticipantLabel = "Player";
                    participationMode = participantCount == 1 ? GameplayPhasePlayerParticipationMode.SoloCanonical : GameplayPhasePlayerParticipationMode.ConfiguredRoster;
                    break;
                }
            }

            if (participantCount <= 0)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseDefinition phaseId='{evt.PhaseId}' has an empty players block.");
            }

            if (string.IsNullOrWhiteSpace(primaryParticipantId))
            {
                primaryParticipantId = "primary_player";
                primaryParticipantLabel = "Player";
            }

            if (participantCount == 1)
            {
                participationMode = GameplayPhasePlayerParticipationMode.SoloCanonical;
            }

            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                participationMode,
                participantCount,
                primaryParticipantId,
                primaryParticipantLabel);
        }

        public static GameplayPhasePlayerParticipationSnapshot SoloCanonical(GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                GameplayPhasePlayerParticipationMode.SoloCanonical,
                1,
                "primary_player",
                "Player");
        }

        public GameplayPhasePlayerParticipationSnapshot(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhasePlayerParticipationMode participationMode,
            int participatingPlayerCount,
            string primaryParticipantId,
            string primaryParticipantLabel)
        {
            PhaseRuntime = phaseRuntime;
            ParticipationMode = participationMode;
            ParticipatingPlayerCount = participatingPlayerCount < 0 ? 0 : participatingPlayerCount;
            PrimaryParticipantId = Normalize(primaryParticipantId);
            PrimaryParticipantLabel = Normalize(primaryParticipantLabel);
            ParticipationSignature = BuildParticipationSignature(
                phaseRuntime,
                participationMode,
                ParticipatingPlayerCount,
                PrimaryParticipantId);
        }

        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public GameplayPhasePlayerParticipationMode ParticipationMode { get; }
        public int ParticipatingPlayerCount { get; }
        public string PrimaryParticipantId { get; }
        public string PrimaryParticipantLabel { get; }
        public string ParticipationSignature { get; }

        public bool HasParticipants => ParticipatingPlayerCount > 0;
        public bool HasCanonicalSoloParticipation => ParticipationMode == GameplayPhasePlayerParticipationMode.SoloCanonical && ParticipatingPlayerCount == 1;
        public bool IsValid => PhaseRuntime.IsValid && HasParticipants && !string.IsNullOrWhiteSpace(PrimaryParticipantId);

        public static GameplayPhasePlayerParticipationSnapshot Empty => new(
            GameplayPhaseRuntimeSnapshot.Empty,
            GameplayPhasePlayerParticipationMode.None,
            0,
            "<none>",
            "<none>");

        public override string ToString()
        {
            return $"phaseRuntime='{PhaseRuntime}', participationMode='{ParticipationMode}', participantCount='{ParticipatingPlayerCount}', primaryId='{PrimaryParticipantId}', participationSignature='{(string.IsNullOrWhiteSpace(ParticipationSignature) ? "<none>" : ParticipationSignature)}'";
        }

        private static string BuildParticipationSignature(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhasePlayerParticipationMode participationMode,
            int participatingPlayerCount,
            string primaryParticipantId)
        {
            string phaseSignature = phaseRuntime.HasPhaseRuntimeSignature ? phaseRuntime.PhaseRuntimeSignature : "<no-phase>";
            string mode = participationMode.ToString();
            string participantId = string.IsNullOrWhiteSpace(primaryParticipantId) ? "<none>" : primaryParticipantId;
            return $"{phaseSignature}|players={mode}:{participatingPlayerCount}|primary={participantId}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }

    /// <summary>
    /// Residual legacy participation view kept for QA and historical compatibility only.
    /// New operational code must use IGameplayParticipationFlowService.
    /// </summary>
    public interface IGameplayPhasePlayerParticipationService
    {
        GameplayPhasePlayerParticipationSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot);
        bool TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot);
        GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot);
        GameplayPhasePlayerParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhasePlayerParticipationService : IGameplayPhasePlayerParticipationService, IDisposable
    {
        private readonly object _sync = new();
        private readonly IGameplayParticipationFlowService _participationFlowService;
        private readonly IGameplayPhaseRuntimeService _phaseRuntimeService;
        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationChangedBinding;
        private GameplayPhasePlayerParticipationSnapshot _current = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _last = GameplayPhasePlayerParticipationSnapshot.Empty;
        private bool _disposed;

        public GameplayPhasePlayerParticipationService(
            IGameplayParticipationFlowService participationFlowService,
            IGameplayPhaseRuntimeService phaseRuntimeService)
        {
            _participationFlowService = participationFlowService ?? throw new ArgumentNullException(nameof(participationFlowService));
            _phaseRuntimeService = phaseRuntimeService ?? throw new ArgumentNullException(nameof(phaseRuntimeService));
            _participationChangedBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationChangedBinding);

            SyncFromCanonicalOwner("initial_sync");

            DebugUtility.LogVerbose<GameplayPhasePlayerParticipationService>(
                "[OBS][GameplaySessionFlow][PlayersLegacy] GameplayPhasePlayerParticipationService registered as a residual compatibility projection.");
        }

        public GameplayPhasePlayerParticipationSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public bool TryGetCurrent(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationService),
                "[FATAL][H1][GameplaySessionFlow] Legacy player-participation write path is not owned by the residual compatibility projection.");
            return GameplayPhasePlayerParticipationSnapshot.Empty;
        }

        public GameplayPhasePlayerParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationService),
                "[FATAL][H1][GameplaySessionFlow] Legacy phase-selected write path is not owned by the residual compatibility projection.");
            return GameplayPhasePlayerParticipationSnapshot.Empty;
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = Normalize(reason);
            string lastSignature;

            lock (_sync)
            {
                _last = _current;
                _current = GameplayPhasePlayerParticipationSnapshot.Empty;
                lastSignature = _last.ParticipationSignature;
            }

            DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                $"[OBS][GameplaySessionFlow][PlayersLegacy] CompatibilityProjectionCleared keepLast='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationChangedBinding);
        }

        private void SyncFromCanonicalOwner(string source)
        {
            if (_participationFlowService.TryGetCurrent(out ParticipationSnapshot current) && current.IsValid)
            {
                SyncProjectionFromCanonicalSnapshot(current, source);
                return;
            }

            if (_participationFlowService.TryGetLast(out ParticipationSnapshot last) && last.IsValid)
            {
                SyncProjectionFromCanonicalSnapshot(last, $"{source}/last");
            }
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            if (evt.IsCleared)
            {
                Clear(evt.Reason);
                return;
            }

            SyncProjectionFromCanonicalSnapshot(evt.Snapshot, evt.Source);
        }

        private void SyncProjectionFromCanonicalSnapshot(ParticipationSnapshot snapshot, string source)
        {
            if (!snapshot.IsValid)
            {
                return;
            }

            GameplayPhaseRuntimeSnapshot phaseRuntime = ResolveCurrentPhaseRuntimeSnapshot();
            GameplayPhasePlayerParticipationSnapshot legacySnapshot = ToLegacySnapshot(snapshot, phaseRuntime);

            lock (_sync)
            {
                _last = _current;
                _current = legacySnapshot;
            }

            DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                $"[OBS][GameplaySessionFlow][PlayersLegacy] CompatibilityProjectionUpdated source='{Normalize(source)}' canonicalSignature='{snapshot.Signature}' legacySignature='{legacySnapshot.ParticipationSignature}' participantCount='{legacySnapshot.ParticipatingPlayerCount}' primaryId='{legacySnapshot.PrimaryParticipantId}'.",
                DebugUtility.Colors.Info);
        }

        private GameplayPhaseRuntimeSnapshot ResolveCurrentPhaseRuntimeSnapshot()
        {
            if (_phaseRuntimeService.TryGetCurrent(out GameplayPhaseRuntimeSnapshot current) && current.IsValid)
            {
                return current;
            }

            if (_phaseRuntimeService.TryGetLast(out GameplayPhaseRuntimeSnapshot last) && last.IsValid)
            {
                return last;
            }

            return GameplayPhaseRuntimeSnapshot.Empty;
        }

        private static GameplayPhasePlayerParticipationSnapshot ToLegacySnapshot(
            ParticipationSnapshot snapshot,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (!snapshot.IsValid || !phaseRuntime.IsValid)
            {
                return GameplayPhasePlayerParticipationSnapshot.Empty;
            }

            int participatingPlayerCount = snapshot.ParticipantCount;
            string primaryParticipantId = snapshot.PrimaryParticipantId.IsValid ? snapshot.PrimaryParticipantId.Value : string.Empty;
            string primaryParticipantLabel = "Player";

            if (snapshot.HasPrimaryParticipant)
            {
                for (int index = 0; index < snapshot.Participants.Length; index += 1)
                {
                    if (snapshot.Participants[index].IsPrimary)
                    {
                        primaryParticipantLabel = snapshot.Participants[index].Kind.ToString();
                        break;
                    }
                }
            }

            GameplayPhasePlayerParticipationMode participationMode =
                participatingPlayerCount == 1
                    ? GameplayPhasePlayerParticipationMode.SoloCanonical
                    : GameplayPhasePlayerParticipationMode.ConfiguredRoster;

            return new GameplayPhasePlayerParticipationSnapshot(
                phaseRuntime,
                participationMode,
                participatingPlayerCount,
                primaryParticipantId,
                primaryParticipantLabel);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}

