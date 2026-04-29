using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.OperationalBinding
{
    /// <summary>
    /// Authoritative mapping boundary between semantic participant identity and materialized runtime actor identity.
    /// </summary>
    public sealed class ActorsParticipantRuntimeMappingService :
        IActorsParticipantRuntimeMappingInPort,
        IActorsParticipantRuntimeMappingQueryPort,
        IActorsMaterializedContinuationQueryPort
    {
        private readonly Dictionary<string, ActorsParticipantRuntimeMappingEntry> _byParticipant = new(StringComparer.Ordinal);
        private readonly Dictionary<AxisActorId, string> _participantByAxis = new(64);
        private readonly Dictionary<RuntimeActorId, string> _participantByRuntime = new(64);

        private ActorsParticipantRuntimeMappingSnapshot _current = ActorsParticipantRuntimeMappingSnapshot.Empty;

        public ActorsParticipantRuntimeMappingSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsParticipantRuntimeMappingSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public bool Upsert(ActorsParticipantRuntimeMappingEntry entry)
        {
            if (!entry.IsValid)
            {
                return false;
            }

            if (HasAxisConflict(entry) || HasRuntimeConflict(entry))
            {
                return false;
            }

            RemoveIndexes(entry.ParticipantId);

            _byParticipant[entry.ParticipantId] = entry;
            _participantByAxis[entry.AxisActorId] = entry.ParticipantId;
            _participantByRuntime[entry.RuntimeActorId] = entry.ParticipantId;

            RebuildSnapshot(entry.Reason);
            return true;
        }

        public bool TryGetByParticipantId(string participantId, out ActorsParticipantRuntimeMappingEntry entry)
        {
            entry = default;
            string normalized = Normalize(participantId);
            return !string.IsNullOrWhiteSpace(normalized) && _byParticipant.TryGetValue(normalized, out entry);
        }

        public bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsParticipantRuntimeMappingEntry entry)
        {
            entry = default;
            if (!axisActorId.IsValid || !_participantByAxis.TryGetValue(axisActorId, out string participantId))
            {
                return false;
            }

            return _byParticipant.TryGetValue(participantId, out entry);
        }

        public bool TryGetByRuntimeActorId(RuntimeActorId runtimeActorId, out ActorsParticipantRuntimeMappingEntry entry)
        {
            entry = default;
            if (!runtimeActorId.IsValid || !_participantByRuntime.TryGetValue(runtimeActorId, out string participantId))
            {
                return false;
            }

            return _byParticipant.TryGetValue(participantId, out entry);
        }

        public bool TryGetAll(List<ActorsParticipantRuntimeMappingEntry> target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            if (_byParticipant.Count == 0)
            {
                return false;
            }

            var ordered = new List<ActorsParticipantRuntimeMappingEntry>(_byParticipant.Values);
            ordered.Sort(static (left, right) => string.CompareOrdinal(left.ParticipantId, right.ParticipantId));
            target.AddRange(ordered);
            return true;
        }

        public void Clear(string reason = null)
        {
            _byParticipant.Clear();
            _participantByAxis.Clear();
            _participantByRuntime.Clear();

            _current = new ActorsParticipantRuntimeMappingSnapshot(
                string.Empty,
                Array.Empty<ActorsParticipantRuntimeMappingEntry>(),
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private bool HasAxisConflict(ActorsParticipantRuntimeMappingEntry entry)
        {
            if (!_participantByAxis.TryGetValue(entry.AxisActorId, out string existingParticipantId))
            {
                return false;
            }

            return !string.Equals(existingParticipantId, entry.ParticipantId, StringComparison.Ordinal);
        }

        private bool HasRuntimeConflict(ActorsParticipantRuntimeMappingEntry entry)
        {
            if (!_participantByRuntime.TryGetValue(entry.RuntimeActorId, out string existingParticipantId))
            {
                return false;
            }

            return !string.Equals(existingParticipantId, entry.ParticipantId, StringComparison.Ordinal);
        }

        private void RemoveIndexes(string participantId)
        {
            if (!_byParticipant.TryGetValue(participantId, out ActorsParticipantRuntimeMappingEntry current))
            {
                return;
            }

            _participantByAxis.Remove(current.AxisActorId);
            _participantByRuntime.Remove(current.RuntimeActorId);
        }

        private void RebuildSnapshot(string reason)
        {
            var entries = new List<ActorsParticipantRuntimeMappingEntry>(_byParticipant.Values);
            entries.Sort(static (left, right) => string.CompareOrdinal(left.ParticipantId, right.ParticipantId));

            string signature = BuildSignature(entries);
            _current = new ActorsParticipantRuntimeMappingSnapshot(
                signature,
                entries.ToArray(),
                string.IsNullOrWhiteSpace(reason) ? "updated" : reason.Trim());
        }

        private static string BuildSignature(List<ActorsParticipantRuntimeMappingEntry> entries)
        {
            var builder = new StringBuilder(256);
            builder.Append("actors-participant-runtime-mapping|count:");
            builder.Append(entries?.Count ?? 0);

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index += 1)
                {
                    ActorsParticipantRuntimeMappingEntry entry = entries[index];
                    builder.Append("|p:");
                    builder.Append(entry.ParticipantId);
                    builder.Append("|a:");
                    builder.Append(entry.AxisActorId.Value);
                    builder.Append("|r:");
                    builder.Append(entry.RuntimeActorId.Value);
                }
            }

            return builder.ToString();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Bridge that publishes authoritative participant/runtime mappings from materialization events.
    /// </summary>
    public sealed class ActorsParticipantRuntimeMappingSpawnBridge : IDisposable
    {
        private readonly IActorsParticipantRuntimeMappingInPort _mappingInPort;
        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private readonly EventBinding<ActorsOperationalMaterializationCompletedEvent> _materializationCompletedBinding;
        private bool _disposed;

        public ActorsParticipantRuntimeMappingSpawnBridge(IActorsParticipantRuntimeMappingInPort mappingInPort)
        {
            _mappingInPort = mappingInPort ?? throw new ArgumentNullException(nameof(mappingInPort));
            _participationBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            _materializationCompletedBinding = new EventBinding<ActorsOperationalMaterializationCompletedEvent>(OnMaterializationCompleted);
            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Register(_materializationCompletedBinding);

            DebugUtility.LogVerbose(typeof(ActorsParticipantRuntimeMappingSpawnBridge),
                "[OBS][ActorsSystem][ParticipantRuntimeMapping] Spawn bridge registrado.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationBinding);
            EventBus<ActorsOperationalMaterializationCompletedEvent>.Unregister(_materializationCompletedBinding);
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed || !evt.IsCleared)
            {
                return;
            }

            if (IsPhaseSelectionClear(evt))
            {
                DebugUtility.LogVerbose(typeof(ActorsParticipantRuntimeMappingSpawnBridge),
                    $"[OBS][ActorsSystem][ParticipantRuntimeMapping] Participation clear transitivo ignorado para preservar continuidade entre phases. source='{evt.Source}' reason='{evt.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _mappingInPort.Clear("participation-cleared");
        }

        private void OnMaterializationCompleted(ActorsOperationalMaterializationCompletedEvent evt)
        {
            if (_disposed || !evt.HasSemanticParticipantId || string.IsNullOrWhiteSpace(evt.ActorId))
            {
                return;
            }

            string participantId = evt.SemanticParticipantId.Trim();
            AxisActorId axisActorId = AxisActorId.FromParticipantId(participantId);
            RuntimeActorId runtimeActorId = new RuntimeActorId(evt.ActorId);
            if (!axisActorId.IsValid || !runtimeActorId.IsValid)
            {
                return;
            }

            _mappingInPort.Upsert(new ActorsParticipantRuntimeMappingEntry(
                participantId,
                axisActorId,
                runtimeActorId,
                source: "GameplayRuntime/ActorsOperationalMaterializationHandoff",
                reason: "runtime-materialized"));
        }

        private static bool IsPhaseSelectionClear(ParticipationSnapshotChangedEvent evt)
        {
            return string.Equals(evt.Reason, "phase_selected", StringComparison.Ordinal);
        }
    }
}
