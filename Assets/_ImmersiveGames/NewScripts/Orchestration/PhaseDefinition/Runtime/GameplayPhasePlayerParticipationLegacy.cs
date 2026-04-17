using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
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
        private GameplayPhasePlayerParticipationSnapshot _current = GameplayPhasePlayerParticipationSnapshot.Empty;
        private GameplayPhasePlayerParticipationSnapshot _last = GameplayPhasePlayerParticipationSnapshot.Empty;

        public GameplayPhasePlayerParticipationService()
        {
            DebugUtility.LogVerbose<GameplayPhasePlayerParticipationService>(
                "[OBS][GameplaySessionFlow][PlayersLegacy] GameplayPhasePlayerParticipationService registrado como ponte de compatibilidade.");
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

        public GameplayPhasePlayerParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return Update(GameplayPhasePlayerParticipationSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        public GameplayPhasePlayerParticipationSnapshot Update(GameplayPhasePlayerParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhasePlayerParticipationService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase player participation snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                    $"[OBS][GameplaySessionFlow][PlayersLegacy] PlayersUpdated compatibilityOnly='true' phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' participationMode='{snapshot.ParticipationMode}' participantCount='{snapshot.ParticipatingPlayerCount}' primaryId='{snapshot.PrimaryParticipantId}' participationSignature='{snapshot.ParticipationSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
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

        public void Clear(string reason = null)
        {
            string normalizedReason = Normalize(reason);
            string lastSignature;

            lock (_sync)
            {
                _current = GameplayPhasePlayerParticipationSnapshot.Empty;
                lastSignature = _last.ParticipationSignature;
            }

            DebugUtility.Log<GameplayPhasePlayerParticipationService>(
                $"[OBS][GameplaySessionFlow][PlayersLegacy] PlayersCleared keepLast='true' compatibilityOnly='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
