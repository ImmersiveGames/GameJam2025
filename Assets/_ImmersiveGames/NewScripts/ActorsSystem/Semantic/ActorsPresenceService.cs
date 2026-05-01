using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    /// <summary>
    /// Slice-2 canonical presence service (ensemble-first, runtime as auxiliary observation input).
    /// </summary>
    public sealed class ActorsPresenceService : IActorsPresenceService
    {
        private readonly IActorsEnsembleService _ensembleService;
        private readonly IActorsRuntimeObservationInPort _runtimeObservationPort;
        private readonly IActorsMaterializedContinuationQueryPort _continuationQueryPort;
        private readonly Dictionary<RuntimeActorId, RuntimeActorObservationRecord> _runtimeLookup = new(64);
        private readonly HashSet<RuntimeActorId> _consumedRuntimeIds = new();
        private ActorsPresenceSnapshot _current = ActorsPresenceSnapshot.Empty;

        public ActorsPresenceService(
            IActorsEnsembleService ensembleService,
            IActorsRuntimeObservationInPort runtimeObservationPort,
            IActorsMaterializedContinuationQueryPort continuationQueryPort)
        {
            _ensembleService = ensembleService ?? throw new ArgumentNullException(nameof(ensembleService));
            _runtimeObservationPort = runtimeObservationPort ?? throw new ArgumentNullException(nameof(runtimeObservationPort));
            _continuationQueryPort = continuationQueryPort ?? throw new ArgumentNullException(nameof(continuationQueryPort));

            DebugUtility.Log(typeof(ActorsPresenceService),
                "[OBS][ActorsSystem] ActorsPresenceService registrado (slice2 canonical presence).",
                DebugUtility.Colors.Info);
        }

        public ActorsPresenceSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsPresenceSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public ActorsPresenceSnapshot Refresh()
        {
            ActorsEnsembleSnapshot ensemble = ResolveEnsembleSnapshot();
            if (!ensemble.IsValid)
            {
                _current = new ActorsPresenceSnapshot(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Array.Empty<ActorsPresenceRecord>(),
                    0,
                    0,
                    0,
                    0,
                    0,
                    "no-ensemble");
                return _current;
            }

            if (!_runtimeObservationPort.TryGetCurrent(out ActorsRuntimeObservationSnapshot runtimeObservation))
            {
                runtimeObservation = ActorsRuntimeObservationSnapshot.Empty;
            }

            if (!_continuationQueryPort.TryGetCurrent(out ActorsParticipantRuntimeMappingSnapshot continuation))
            {
                continuation = ActorsParticipantRuntimeMappingSnapshot.Empty;
            }

            BuildRuntimeLookup(runtimeObservation);
            _consumedRuntimeIds.Clear();

            ActorIdentityRecord[] members = ensemble.Members ?? Array.Empty<ActorIdentityRecord>();
            var entries = new List<ActorsPresenceRecord>(members.Length);
            int expectedCount = 0;
            int materializedCount = 0;
            int absentCount = 0;
            int inconsistentCount = 0;

            for (int index = 0; index < members.Length; index += 1)
            {
                ActorIdentityRecord member = members[index];
                if (!member.IsValid)
                {
                    continue;
                }

                bool isExpected = true;
                expectedCount += 1;

                bool hasContinuation = _continuationQueryPort.TryGetByAxisActorId(member.AxisActorId, out ActorsParticipantRuntimeMappingEntry continuationEntry);
                RuntimeActorId continuationRuntimeId = hasContinuation ? continuationEntry.RuntimeActorId : RuntimeActorId.None;
                RuntimeActorId expectedRuntimeHint = ResolveExpectedRuntimeHint(member, continuationRuntimeId);
                bool hasExpectedHint = expectedRuntimeHint.IsValid;
                bool isMaterialized = TryResolveMaterializedRuntime(member, continuationRuntimeId, expectedRuntimeHint, out RuntimeActorId materializedRuntimeId);
                bool isInconsistent = hasContinuation &&
                                      continuationRuntimeId.IsValid &&
                                      isMaterialized &&
                                      materializedRuntimeId.IsValid &&
                                      materializedRuntimeId != continuationRuntimeId;

                if (isMaterialized)
                {
                    materializedCount += 1;
                    _consumedRuntimeIds.Add(materializedRuntimeId);
                }
                else
                {
                    absentCount += 1;
                }

                if (isInconsistent)
                {
                    inconsistentCount += 1;
                }

                ActorPresenceStatus status = ResolveStatus(isExpected, isMaterialized, isInconsistent);
                string reason = BuildEntryReason(
                    isMaterialized,
                    isInconsistent,
                    hasExpectedHint,
                    expectedRuntimeHint,
                    materializedRuntimeId,
                    hasContinuation,
                    continuationRuntimeId);

                entries.Add(new ActorsPresenceRecord(
                    member.AxisActorId,
                    member.Role,
                    member.Relevance,
                    isExpected,
                    isMaterialized,
                    isInconsistent,
                    isMaterialized ? materializedRuntimeId : RuntimeActorId.None,
                    member.SemanticParticipantId,
                    status,
                    reason));
            }

            int runtimeOrphanCount = ComputeRuntimeOrphanCount(runtimeObservation);
            if (runtimeOrphanCount > 0)
            {
                inconsistentCount += runtimeOrphanCount;
            }

            string presenceSignature = BuildPresenceSignature(
                ensemble.EnsembleSignature,
                runtimeObservation.ObservationSignature,
                continuation.Signature,
                entries.Count,
                materializedCount,
                absentCount,
                inconsistentCount,
                runtimeOrphanCount);

            string snapshotReason = ResolveSnapshotReason(entries.Count, inconsistentCount, runtimeOrphanCount);

            _current = new ActorsPresenceSnapshot(
                ensemble.EnsembleSignature,
                runtimeObservation.ObservationSignature,
                presenceSignature,
                entries.ToArray(),
                expectedCount,
                materializedCount,
                absentCount,
                inconsistentCount,
                runtimeOrphanCount,
                snapshotReason);

            return _current;
        }

        public void Clear(string reason = null)
        {
            _current = new ActorsPresenceSnapshot(
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<ActorsPresenceRecord>(),
                0,
                0,
                0,
                0,
                0,
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private ActorsEnsembleSnapshot ResolveEnsembleSnapshot()
        {
            if (_ensembleService.TryGetCurrent(out ActorsEnsembleSnapshot current) && current.IsValid)
            {
                return current;
            }

            return _ensembleService.Refresh();
        }

        private void BuildRuntimeLookup(ActorsRuntimeObservationSnapshot runtimeObservation)
        {
            _runtimeLookup.Clear();
            RuntimeActorObservationRecord[] runtimeActors = runtimeObservation.RuntimeActors ?? Array.Empty<RuntimeActorObservationRecord>();
            for (int index = 0; index < runtimeActors.Length; index += 1)
            {
                RuntimeActorObservationRecord record = runtimeActors[index];
                if (!record.IsValid || _runtimeLookup.ContainsKey(record.RuntimeActorId))
                {
                    continue;
                }

                if (!record.IsActive)
                {
                    continue;
                }

                _runtimeLookup.Add(record.RuntimeActorId, record);
            }
        }

        private static RuntimeActorId ResolveExpectedRuntimeHint(ActorIdentityRecord member, RuntimeActorId continuationRuntimeId)
        {
            if (continuationRuntimeId.IsValid)
            {
                return continuationRuntimeId;
            }

            if (member.RuntimeActorId.IsValid)
            {
                return member.RuntimeActorId;
            }

            return RuntimeActorId.None;
        }

        private bool TryResolveMaterializedRuntime(
            ActorIdentityRecord member,
            RuntimeActorId continuationRuntimeId,
            RuntimeActorId expectedRuntimeHint,
            out RuntimeActorId runtimeActorId)
        {
            runtimeActorId = RuntimeActorId.None;

            if (continuationRuntimeId.IsValid &&
                _runtimeLookup.TryGetValue(continuationRuntimeId, out RuntimeActorObservationRecord continuedRecord) &&
                IsUsableRuntimeRecord(continuedRecord))
            {
                runtimeActorId = continuedRecord.RuntimeActorId;
                return true;
            }

            if (expectedRuntimeHint.IsValid &&
                _runtimeLookup.TryGetValue(expectedRuntimeHint, out RuntimeActorObservationRecord hintedRecord) &&
                IsUsableRuntimeRecord(hintedRecord))
            {
                runtimeActorId = hintedRecord.RuntimeActorId;
                return true;
            }

            if (member.RuntimeActorId.IsValid &&
                _runtimeLookup.TryGetValue(member.RuntimeActorId, out RuntimeActorObservationRecord exactRecord) &&
                IsUsableRuntimeRecord(exactRecord))
            {
                runtimeActorId = exactRecord.RuntimeActorId;
                return true;
            }

            return false;
        }

        private static bool IsUsableRuntimeRecord(RuntimeActorObservationRecord record)
        {
            return record.IsValid && record.IsActive;
        }

        private static ActorPresenceStatus ResolveStatus(bool isExpected, bool isMaterialized, bool isInconsistent)
        {
            if (isInconsistent)
            {
                return ActorPresenceStatus.Inconsistent;
            }

            if (isExpected && isMaterialized)
            {
                return ActorPresenceStatus.ExpectedMaterialized;
            }

            if (isExpected)
            {
                return ActorPresenceStatus.ExpectedNotMaterialized;
            }

            return ActorPresenceStatus.UnexpectedMaterialized;
        }

        private static string BuildEntryReason(
            bool isMaterialized,
            bool isInconsistent,
            bool hasExpectedHint,
            RuntimeActorId expectedRuntimeHint,
            RuntimeActorId materializedRuntimeId,
            bool hasContinuation,
            RuntimeActorId continuationRuntimeId)
        {
            if (isInconsistent)
            {
                return hasContinuation
                    ? $"runtime-mismatch continuation='{continuationRuntimeId}' materialized='{materializedRuntimeId}'"
                    : hasExpectedHint
                        ? $"runtime-mismatch expected='{expectedRuntimeHint}' materialized='{materializedRuntimeId}'"
                        : "runtime-mismatch";
            }

            if (isMaterialized)
            {
                return hasContinuation
                    ? $"materialized-via-continuation runtime='{materializedRuntimeId}'"
                    : "materialized";
            }

            return hasContinuation && continuationRuntimeId.IsValid
                ? $"continuation-runtime-missing runtime='{continuationRuntimeId}'"
                : "not-materialized";
        }

        private int ComputeRuntimeOrphanCount(ActorsRuntimeObservationSnapshot runtimeObservation)
        {
            int orphanCount = 0;
            RuntimeActorObservationRecord[] runtimeActors = runtimeObservation.RuntimeActors ?? Array.Empty<RuntimeActorObservationRecord>();
            for (int index = 0; index < runtimeActors.Length; index += 1)
            {
                RuntimeActorObservationRecord record = runtimeActors[index];
                if (!record.IsValid)
                {
                    continue;
                }

                if (!_consumedRuntimeIds.Contains(record.RuntimeActorId))
                {
                    orphanCount += 1;
                }
            }

            return orphanCount;
        }

        private static string BuildPresenceSignature(
            string ensembleSignature,
            string runtimeObservationSignature,
            string continuationSignature,
            int entryCount,
            int materializedCount,
            int absentCount,
            int inconsistentCount,
            int runtimeOrphanCount)
        {
            var builder = new StringBuilder(256);
            builder.Append(string.IsNullOrWhiteSpace(ensembleSignature) ? "<no-ensemble>" : ensembleSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(runtimeObservationSignature) ? "<no-runtime>" : runtimeObservationSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(continuationSignature) ? "<no-continuation>" : continuationSignature.Trim());
            builder.Append('|');
            builder.Append("entries:");
            builder.Append(entryCount);
            builder.Append("|mat:");
            builder.Append(materializedCount);
            builder.Append("|abs:");
            builder.Append(absentCount);
            builder.Append("|inc:");
            builder.Append(inconsistentCount);
            builder.Append("|orphans:");
            builder.Append(runtimeOrphanCount);
            return builder.ToString();
        }

        private static string ResolveSnapshotReason(int entryCount, int inconsistentCount, int runtimeOrphanCount)
        {
            if (entryCount <= 0)
            {
                return "no-members";
            }

            if (inconsistentCount > 0 || runtimeOrphanCount > 0)
            {
                return "inconsistent";
            }

            return "resolved";
        }
    }

    /// <summary>
    /// Architectural registry boundary for ActorsSystem (ensemble-first canonical projection).
    /// </summary>
    public sealed class ActorsRegistryBoundaryService : IActorsRegistryBoundary
    {
        private readonly IActorsPresenceService _presenceService;
        private string _projectedPresenceSignature = string.Empty;
        private readonly List<ActorsRegistryEntry> _entries = new(64);
        private readonly Dictionary<AxisActorId, ActorsRegistryEntry> _index = new(64);

        public ActorsRegistryBoundaryService(IActorsPresenceService presenceService)
        {
            _presenceService = presenceService ?? throw new ArgumentNullException(nameof(presenceService));

            DebugUtility.Log(typeof(ActorsRegistryBoundaryService),
                "[OBS][ActorsSystem] ActorsRegistryBoundaryService registrado (slice2 architectural boundary).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsRegistryEntry entry)
        {
            entry = default;
            if (!axisActorId.IsValid)
            {
                return false;
            }

            if (!EnsureProjection())
            {
                return false;
            }

            return _index.TryGetValue(axisActorId, out entry);
        }

        public bool TryGetAll(List<ActorsRegistryEntry> target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            if (!EnsureProjection())
            {
                return false;
            }

            target.AddRange(_entries);
            return true;
        }

        private bool EnsureProjection()
        {
            ActorsPresenceSnapshot presence = ResolvePresenceSnapshot();
            if (!presence.IsValid)
            {
                _entries.Clear();
                _index.Clear();
                _projectedPresenceSignature = string.Empty;
                return false;
            }

            if (string.Equals(_projectedPresenceSignature, presence.PresenceSignature, StringComparison.Ordinal))
            {
                return true;
            }

            _entries.Clear();
            _index.Clear();

            ActorsPresenceRecord[] records = presence.Entries ?? Array.Empty<ActorsPresenceRecord>();
            for (int index = 0; index < records.Length; index += 1)
            {
                ActorsPresenceRecord record = records[index];
                if (!record.IsValid)
                {
                    continue;
                }

                var entry = new ActorsRegistryEntry(
                    record.AxisActorId,
                    record.Role,
                    record.Relevance,
                    record.IsExpected,
                    record.IsMaterialized,
                    record.IsInconsistent,
                    record.RuntimeActorId,
                    record.SemanticParticipantId,
                    record.Status,
                    record.Reason);

                _entries.Add(entry);
                _index[entry.AxisActorId] = entry;
            }

            _projectedPresenceSignature = presence.PresenceSignature;
            return true;
        }

        private ActorsPresenceSnapshot ResolvePresenceSnapshot()
        {
            if (_presenceService.TryGetCurrent(out ActorsPresenceSnapshot current) && current.IsValid)
            {
                return current;
            }

            return _presenceService.Refresh();
        }
    }
}
