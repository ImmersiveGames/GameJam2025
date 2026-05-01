using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.OperationalBinding
{
    /// <summary>
    /// Canonical operational binding boundary for ActorsSystem.
    /// Keeps explicit links between semantic/canonical IDs and Unity operational handles without transferring ownership.
    /// </summary>
    public sealed class ActorsOperationalBindingService :
        IActorsOperationalBindingInPort,
        IActorsOperationalBindingQueryPort
    {
        private readonly Dictionary<AxisActorId, ActorsOperationalBindingEntry> _byAxisActor = new(64);
        private readonly Dictionary<string, AxisActorId> _participantIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<RuntimeActorId, AxisActorId> _runtimeIndex = new(64);

        private ActorsOperationalBindingSnapshot _current = ActorsOperationalBindingSnapshot.Empty;

        public ActorsOperationalBindingService()
        {
            DebugUtility.Log(typeof(ActorsOperationalBindingService),
                "[OBS][ActorsSystem][OperationalBinding] ActorsOperationalBindingService registrado (boundary explicito entre semantica e runtime operacional Unity).",
                DebugUtility.Colors.Info);
        }

        public ActorsOperationalBindingSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsOperationalBindingSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public bool Upsert(ActorsOperationalBindingEntry entry)
        {
            if (!entry.IsValid)
            {
                ReportConflict(new ActorsBindingConflict(
                    ActorsBindingConflictCode.InvalidBindingState,
                    entry.ParticipantId,
                    entry.AxisActorId,
                    entry.RuntimeActorId,
                    entry.Source,
                    "invalid_operational_binding_entry"),
                    failFast: false);
                return false;
            }

            if (HasParticipantConflict(entry, out ActorsBindingConflict participantConflict))
            {
                ReportConflict(participantConflict, failFast: true);
                return false;
            }

            if (HasAxisConflict(entry, out ActorsBindingConflict axisConflict))
            {
                ReportConflict(axisConflict, failFast: true);
                return false;
            }

            if (HasRuntimeConflict(entry, out ActorsBindingConflict runtimeConflict))
            {
                ReportConflict(runtimeConflict, failFast: true);
                return false;
            }

            RemoveSecondaryIndexesForAxis(entry.AxisActorId);

            _byAxisActor[entry.AxisActorId] = entry;
            _participantIndex[entry.ParticipantId] = entry.AxisActorId;
            if (entry.RuntimeActorId.IsValid)
            {
                _runtimeIndex[entry.RuntimeActorId] = entry.AxisActorId;
            }

            RebuildSnapshot(entry.Reason);
            return true;
        }

        public bool TrySetState(AxisActorId axisActorId, ActorsOperationalBindingState nextState, string reason = null, string source = null)
        {
            if (!axisActorId.IsValid ||
                !_byAxisActor.TryGetValue(axisActorId, out ActorsOperationalBindingEntry current))
            {
                return false;
            }

            if (!ActorsOperationalBindingEntry.TryTransitionState(current, nextState, reason, source, out ActorsOperationalBindingEntry next))
            {
                return false;
            }

            _byAxisActor[axisActorId] = next;
            RebuildSnapshot(reason);
            return true;
        }

        public bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsOperationalBindingEntry entry)
        {
            entry = default;
            return axisActorId.IsValid && _byAxisActor.TryGetValue(axisActorId, out entry);
        }

        public bool TryGetByParticipantId(string participantId, out ActorsOperationalBindingEntry entry)
        {
            entry = default;
            string normalizedParticipantId = Normalize(participantId);
            if (string.IsNullOrWhiteSpace(normalizedParticipantId))
            {
                return false;
            }

            if (!_participantIndex.TryGetValue(normalizedParticipantId, out AxisActorId axisActorId))
            {
                return false;
            }

            return _byAxisActor.TryGetValue(axisActorId, out entry);
        }

        public bool TryGetByRuntimeActorId(RuntimeActorId runtimeActorId, out ActorsOperationalBindingEntry entry)
        {
            entry = default;
            if (!runtimeActorId.IsValid)
            {
                return false;
            }

            if (!_runtimeIndex.TryGetValue(runtimeActorId, out AxisActorId axisActorId))
            {
                return false;
            }

            return _byAxisActor.TryGetValue(axisActorId, out entry);
        }

        public bool TryGetAll(List<ActorsOperationalBindingEntry> target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            if (_byAxisActor.Count == 0)
            {
                return false;
            }

            var ordered = new List<ActorsOperationalBindingEntry>(_byAxisActor.Values);
            ordered.Sort(static (left, right) => string.CompareOrdinal(left.AxisActorId.Value, right.AxisActorId.Value));
            target.AddRange(ordered);
            return true;
        }

        public void Clear(string reason = null)
        {
            _byAxisActor.Clear();
            _participantIndex.Clear();
            _runtimeIndex.Clear();

            _current = new ActorsOperationalBindingSnapshot(
                string.Empty,
                Array.Empty<ActorsOperationalBindingEntry>(),
                0,
                0,
                0,
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private bool HasParticipantConflict(ActorsOperationalBindingEntry entry, out ActorsBindingConflict conflict)
        {
            conflict = default;
            if (!_participantIndex.TryGetValue(entry.ParticipantId, out AxisActorId existingAxisActorId))
            {
                return false;
            }

            if (existingAxisActorId == entry.AxisActorId)
            {
                return false;
            }

            conflict = new ActorsBindingConflict(
                ActorsBindingConflictCode.DuplicateParticipant,
                entry.ParticipantId,
                entry.AxisActorId,
                entry.RuntimeActorId,
                entry.Source,
                "participant_already_bound_to_other_axis_actor");
            return true;
        }

        private bool HasAxisConflict(ActorsOperationalBindingEntry entry, out ActorsBindingConflict conflict)
        {
            conflict = default;
            if (!_byAxisActor.TryGetValue(entry.AxisActorId, out ActorsOperationalBindingEntry existing))
            {
                return false;
            }

            if (string.Equals(existing.ParticipantId, entry.ParticipantId, StringComparison.Ordinal))
            {
                return false;
            }

            conflict = new ActorsBindingConflict(
                ActorsBindingConflictCode.DuplicateAxisActorId,
                entry.ParticipantId,
                entry.AxisActorId,
                entry.RuntimeActorId,
                entry.Source,
                "axis_actor_id_already_bound_to_other_participant");
            return true;
        }

        private bool HasRuntimeConflict(ActorsOperationalBindingEntry entry, out ActorsBindingConflict conflict)
        {
            conflict = default;
            if (!entry.RuntimeActorId.IsValid)
            {
                return false;
            }

            if (!_runtimeIndex.TryGetValue(entry.RuntimeActorId, out AxisActorId existingAxisActorId))
            {
                return false;
            }

            if (existingAxisActorId == entry.AxisActorId)
            {
                return false;
            }

            conflict = new ActorsBindingConflict(
                ActorsBindingConflictCode.DuplicateRuntimeActorId,
                entry.ParticipantId,
                entry.AxisActorId,
                entry.RuntimeActorId,
                entry.Source,
                "runtime_actor_id_already_bound_to_other_axis_actor");
            return true;
        }

        private void RemoveSecondaryIndexesForAxis(AxisActorId axisActorId)
        {
            if (!axisActorId.IsValid ||
                !_byAxisActor.TryGetValue(axisActorId, out ActorsOperationalBindingEntry existing))
            {
                return;
            }

            _participantIndex.Remove(existing.ParticipantId);
            if (existing.RuntimeActorId.IsValid)
            {
                _runtimeIndex.Remove(existing.RuntimeActorId);
            }
        }

        private void RebuildSnapshot(string reason)
        {
            var entries = new List<ActorsOperationalBindingEntry>(_byAxisActor.Values);
            entries.Sort(static (left, right) => string.CompareOrdinal(left.AxisActorId.Value, right.AxisActorId.Value));

            int boundCount = 0;
            int activeCount = 0;
            int disconnectedCount = 0;

            for (int index = 0; index < entries.Count; index += 1)
            {
                ActorsOperationalBindingState state = entries[index].State;
                if (state == ActorsOperationalBindingState.Bound)
                {
                    boundCount += 1;
                }
                else if (state == ActorsOperationalBindingState.Active)
                {
                    activeCount += 1;
                }
                else if (state == ActorsOperationalBindingState.Disconnected)
                {
                    disconnectedCount += 1;
                }
            }

            string signature = BuildSnapshotSignature(entries, boundCount, activeCount, disconnectedCount);
            _current = new ActorsOperationalBindingSnapshot(
                signature,
                entries.ToArray(),
                boundCount,
                activeCount,
                disconnectedCount,
                string.IsNullOrWhiteSpace(reason) ? "updated" : reason.Trim());
        }

        private static string BuildSnapshotSignature(
            List<ActorsOperationalBindingEntry> entries,
            int boundCount,
            int activeCount,
            int disconnectedCount)
        {
            var builder = new StringBuilder(256);
            builder.Append("actors-operational-binding|count:");
            builder.Append(entries?.Count ?? 0);
            builder.Append("|bound:");
            builder.Append(boundCount);
            builder.Append("|active:");
            builder.Append(activeCount);
            builder.Append("|disconnected:");
            builder.Append(disconnectedCount);

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index += 1)
                {
                    ActorsOperationalBindingEntry entry = entries[index];
                    builder.Append("|a:");
                    builder.Append(entry.AxisActorId.Value);
                    builder.Append("|p:");
                    builder.Append(entry.ParticipantId);
                    builder.Append("|r:");
                    builder.Append(entry.RuntimeActorId.Value);
                    builder.Append("|s:");
                    builder.Append((int)entry.State);
                    builder.Append("|f:");
                    builder.Append((int)entry.FlowStep);
                    builder.Append("|pi:");
                    builder.Append(entry.UnityHandles.HasPlayerIndex ? entry.UnityHandles.PlayerIndex.Value.ToString() : "<none>");
                    builder.Append("|iu:");
                    builder.Append(entry.UnityHandles.HasInputUserId ? entry.UnityHandles.InputUserId.Value.ToString() : "<none>");
                }
            }

            return builder.ToString();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private static void ReportConflict(ActorsBindingConflict conflict, bool failFast)
        {
            if (!conflict.IsValid)
            {
                return;
            }

            string message =
                $"[FATAL][ActorsSystem][OperationalBinding] conflict code='{conflict.Code}' participantId='{AsText(conflict.ParticipantId)}' axisActorId='{conflict.AxisActorId}' runtimeActorId='{conflict.RuntimeActorId}' source='{AsText(conflict.Source)}' reason='{AsText(conflict.Reason)}'.";
            DebugUtility.LogError(typeof(ActorsOperationalBindingService), message);
            EventBus<ActorsBindingConflictEvent>.Raise(new ActorsBindingConflictEvent(conflict));

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
