using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    /// <summary>
    /// Canonical semantic service that derives actor materialization intent/spec.
    /// It does not execute spawn/despawn; it only publishes what the axis expects.
    /// </summary>
    public sealed class ActorsMaterializationPlanService : IActorsMaterializationPlanService
    {
        private readonly IActorsEnsembleService _ensembleService;
        private readonly IActorsPresenceService _presenceService;
        private readonly IActorsRuntimeObservationInPort _runtimeObservationPort;
        private readonly IActorsOperationalBindingQueryPort _operationalBindingQueryPort;
        private readonly Dictionary<AxisActorId, ActorsPresenceRecord> _presenceByAxis = new(64);
        private readonly Dictionary<AxisActorId, ActorsOperationalBindingEntry> _bindingByAxis = new(64);
        private readonly HashSet<RuntimeActorId> _axisRuntimeIds = new();
        private ActorsMaterializationPlanSnapshot _current = ActorsMaterializationPlanSnapshot.Empty;

        public ActorsMaterializationPlanService(
            IActorsEnsembleService ensembleService,
            IActorsPresenceService presenceService,
            IActorsRuntimeObservationInPort runtimeObservationPort,
            IActorsOperationalBindingQueryPort operationalBindingQueryPort)
        {
            _ensembleService = ensembleService ?? throw new ArgumentNullException(nameof(ensembleService));
            _presenceService = presenceService ?? throw new ArgumentNullException(nameof(presenceService));
            _runtimeObservationPort = runtimeObservationPort ?? throw new ArgumentNullException(nameof(runtimeObservationPort));
            _operationalBindingQueryPort = operationalBindingQueryPort ?? throw new ArgumentNullException(nameof(operationalBindingQueryPort));

            DebugUtility.Log(typeof(ActorsMaterializationPlanService),
                "[OBS][ActorsSystem] ActorsMaterializationPlanService registrado (slice materialization plan/spec).",
                DebugUtility.Colors.Info);
        }

        public ActorsMaterializationPlanSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsMaterializationPlanSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public ActorsMaterializationPlanSnapshot Refresh()
        {
            ActorsEnsembleSnapshot ensemble = ResolveEnsembleSnapshot();
            if (!ensemble.IsValid)
            {
                _current = BuildInvalidSnapshot("no-ensemble", ensemble.EnsembleSignature, string.Empty, string.Empty, string.Empty);
                return _current;
            }

            ActorsPresenceSnapshot presence = ResolvePresenceSnapshot();
            if (!presence.IsValid)
            {
                _current = BuildInvalidSnapshot("no-presence", ensemble.EnsembleSignature, presence.PresenceSignature, string.Empty, string.Empty);
                return _current;
            }

            if (!_runtimeObservationPort.TryGetCurrent(out ActorsRuntimeObservationSnapshot runtimeObservation))
            {
                runtimeObservation = ActorsRuntimeObservationSnapshot.Empty;
            }

            if (!_operationalBindingQueryPort.TryGetCurrent(out ActorsOperationalBindingSnapshot operationalBinding))
            {
                operationalBinding = ActorsOperationalBindingSnapshot.Empty;
            }

            BuildPresenceLookup(presence);
            BuildBindingLookup(operationalBinding);
            _axisRuntimeIds.Clear();

            ActorIdentityRecord[] members = ensemble.Members ?? Array.Empty<ActorIdentityRecord>();
            var entries = new List<ActorsMaterializationSpecEntry>(members.Length + Math.Max(0, presence.RuntimeOrphanCount));

            for (int index = 0; index < members.Length; index += 1)
            {
                ActorIdentityRecord member = members[index];
                if (!member.IsValid)
                {
                    continue;
                }

                ActorsPresenceRecord presenceRecord = ResolvePresenceRecord(member);
                if (presenceRecord.IsMaterialized && presenceRecord.RuntimeActorId.IsValid)
                {
                    _axisRuntimeIds.Add(presenceRecord.RuntimeActorId);
                }

                bool hasBinding = _bindingByAxis.TryGetValue(member.AxisActorId, out ActorsOperationalBindingEntry bindingEntry);
                ActorsOperationalBindingState bindingState = hasBinding ? bindingEntry.State : ActorsOperationalBindingState.Unbound;
                ActorMaterializationIntent intent = ResolveIntent(member, presenceRecord);
                ActorMaterializationClassification classification = ResolveAxisClassification(intent);
                string reason = BuildAxisEntryReason(member, presenceRecord, hasBinding, bindingState);

                entries.Add(new ActorsMaterializationSpecEntry(
                    ActorMaterializationSpecKind.AxisActor,
                    member.AxisActorId,
                    presenceRecord.IsMaterialized ? presenceRecord.RuntimeActorId : RuntimeActorId.None,
                    member.Role,
                    member.OperationalRecipeKind,
                    member.Relevance,
                    member.SemanticParticipantId,
                    member.ActorSpecId,
                    member.ActorSetRef,
                    presenceRecord.Status,
                    presenceRecord.IsExpected,
                    presenceRecord.IsMaterialized,
                    presenceRecord.IsInconsistent,
                    hasBinding,
                    bindingState,
                    intent,
                    classification,
                    reason));
            }

            AddRuntimeOrphans(runtimeObservation, entries);

            var counters = CountIntents(entries);
            string planSignature = BuildPlanSignature(
                ensemble.EnsembleSignature,
                presence.PresenceSignature,
                runtimeObservation.ObservationSignature,
                operationalBinding.Signature,
                entries,
                counters.keepCount,
                counters.materializeCount,
                counters.rematerializeCount,
                counters.observeWithoutActionCount,
                counters.inconsistentCount,
                counters.excessOrphanCount);

            string snapshotReason = ResolveSnapshotReason(entries);

            _current = new ActorsMaterializationPlanSnapshot(
                ensemble.EnsembleSignature,
                presence.PresenceSignature,
                runtimeObservation.ObservationSignature,
                operationalBinding.Signature,
                planSignature,
                entries.ToArray(),
                counters.keepCount,
                counters.materializeCount,
                counters.rematerializeCount,
                counters.observeWithoutActionCount,
                counters.inconsistentCount,
                counters.excessOrphanCount,
                snapshotReason);

            return _current;
        }

        public void Clear(string reason = null)
        {
            _current = new ActorsMaterializationPlanSnapshot(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<ActorsMaterializationSpecEntry>(),
                0,
                0,
                0,
                0,
                0,
                0,
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private void AddRuntimeOrphans(ActorsRuntimeObservationSnapshot runtimeObservation, List<ActorsMaterializationSpecEntry> entries)
        {
            RuntimeActorObservationRecord[] runtimeActors = runtimeObservation.RuntimeActors ?? Array.Empty<RuntimeActorObservationRecord>();
            var orphans = new List<RuntimeActorObservationRecord>(runtimeActors.Length);

            for (int index = 0; index < runtimeActors.Length; index += 1)
            {
                RuntimeActorObservationRecord runtimeRecord = runtimeActors[index];
                if (!runtimeRecord.IsValid || _axisRuntimeIds.Contains(runtimeRecord.RuntimeActorId))
                {
                    continue;
                }

                orphans.Add(runtimeRecord);
            }

            orphans.Sort(static (left, right) => string.CompareOrdinal(left.RuntimeActorId.Value, right.RuntimeActorId.Value));

            for (int index = 0; index < orphans.Count; index += 1)
            {
                RuntimeActorObservationRecord orphan = orphans[index];
                bool isProblematicOrphan = orphan.IsActive;
                ActorMaterializationClassification classification = isProblematicOrphan
                    ? ActorMaterializationClassification.RuntimeOrphanProblematic
                    : ActorMaterializationClassification.RuntimeOrphanTolerated;
                entries.Add(new ActorsMaterializationSpecEntry(
                    ActorMaterializationSpecKind.RuntimeOrphan,
                    AxisActorId.None,
                    orphan.RuntimeActorId,
                    orphan.ObservedRole,
                    ActorOperationalRecipeKind.Unknown,
                    ActorRelevance.Observed,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    ActorPresenceStatus.UnexpectedMaterialized,
                    isExpected: false,
                    isMaterialized: true,
                    isInconsistent: isProblematicOrphan,
                    hasOperationalBinding: false,
                    operationalBindingState: ActorsOperationalBindingState.Unbound,
                    intent: ActorMaterializationIntent.ExcessOrphan,
                    classification: classification,
                    reason: isProblematicOrphan ? "runtime-orphan-active-problematic" : "runtime-orphan-inactive-tolerated"));
            }
        }

        private static ActorMaterializationIntent ResolveIntent(ActorIdentityRecord member, ActorsPresenceRecord presence)
        {
            if (presence.IsInconsistent && presence.IsMaterialized)
            {
                return ActorMaterializationIntent.Rematerialize;
            }

            if (presence.IsInconsistent)
            {
                return ActorMaterializationIntent.Inconsistent;
            }

            if (!presence.IsMaterialized)
            {
                return member.Relevance == ActorRelevance.Observed
                    ? ActorMaterializationIntent.ObserveWithoutAction
                    : ActorMaterializationIntent.Materialize;
            }

            return ActorMaterializationIntent.Keep;
        }

        private static string BuildAxisEntryReason(
            ActorIdentityRecord member,
            ActorsPresenceRecord presence,
            bool hasBinding,
            ActorsOperationalBindingState bindingState)
        {
            string binding = hasBinding ? bindingState.ToString() : "none";
            return $"presence='{presence.Status}' relevance='{member.Relevance}' binding='{binding}' reason='{(string.IsNullOrWhiteSpace(presence.Reason) ? "<none>" : presence.Reason)}'";
        }

        private static ActorMaterializationClassification ResolveAxisClassification(ActorMaterializationIntent intent)
        {
            return intent switch
            {
                ActorMaterializationIntent.Keep => ActorMaterializationClassification.StableNoAction,
                ActorMaterializationIntent.ObserveWithoutAction => ActorMaterializationClassification.ObserveNoAction,
                ActorMaterializationIntent.Materialize => ActorMaterializationClassification.RequiresExecutionMaterialize,
                ActorMaterializationIntent.Rematerialize => ActorMaterializationClassification.RequiresExecutionRematerialize,
                ActorMaterializationIntent.Inconsistent => ActorMaterializationClassification.InconsistentNoAutoRemediation,
                _ => ActorMaterializationClassification.Unknown
            };
        }

        private static (int keepCount, int materializeCount, int rematerializeCount, int observeWithoutActionCount, int inconsistentCount, int excessOrphanCount) CountIntents(List<ActorsMaterializationSpecEntry> entries)
        {
            int keepCount = 0;
            int materializeCount = 0;
            int rematerializeCount = 0;
            int observeWithoutActionCount = 0;
            int inconsistentCount = 0;
            int excessOrphanCount = 0;

            for (int index = 0; index < entries.Count; index += 1)
            {
                switch (entries[index].Intent)
                {
                    case ActorMaterializationIntent.Keep:
                        keepCount += 1;
                        break;
                    case ActorMaterializationIntent.Materialize:
                        materializeCount += 1;
                        break;
                    case ActorMaterializationIntent.Rematerialize:
                        rematerializeCount += 1;
                        break;
                    case ActorMaterializationIntent.ObserveWithoutAction:
                        observeWithoutActionCount += 1;
                        break;
                    case ActorMaterializationIntent.Inconsistent:
                        inconsistentCount += 1;
                        break;
                    case ActorMaterializationIntent.ExcessOrphan:
                        excessOrphanCount += 1;
                        break;
                }
            }

            return (keepCount, materializeCount, rematerializeCount, observeWithoutActionCount, inconsistentCount, excessOrphanCount);
        }

        private static string BuildPlanSignature(
            string ensembleSignature,
            string presenceSignature,
            string runtimeObservationSignature,
            string operationalBindingSignature,
            List<ActorsMaterializationSpecEntry> entries,
            int keepCount,
            int materializeCount,
            int rematerializeCount,
            int observeWithoutActionCount,
            int inconsistentCount,
            int excessOrphanCount)
        {
            var builder = new StringBuilder(512);
            builder.Append(string.IsNullOrWhiteSpace(ensembleSignature) ? "<no-ensemble>" : ensembleSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(presenceSignature) ? "<no-presence>" : presenceSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(runtimeObservationSignature) ? "<no-runtime>" : runtimeObservationSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(operationalBindingSignature) ? "<no-binding>" : operationalBindingSignature.Trim());
            builder.Append("|count:");
            builder.Append(entries.Count);
            builder.Append("|keep:");
            builder.Append(keepCount);
            builder.Append("|mat:");
            builder.Append(materializeCount);
            builder.Append("|remat:");
            builder.Append(rematerializeCount);
            builder.Append("|obs:");
            builder.Append(observeWithoutActionCount);
            builder.Append("|inc:");
            builder.Append(inconsistentCount);
            builder.Append("|orph:");
            builder.Append(excessOrphanCount);

            for (int index = 0; index < entries.Count; index += 1)
            {
                ActorsMaterializationSpecEntry entry = entries[index];
                builder.Append("|e:");
                builder.Append(entry.Kind);
                builder.Append(':');
                builder.Append(entry.AxisActorId.IsValid ? entry.AxisActorId.Value : "<none>");
                builder.Append(':');
                builder.Append(entry.RuntimeActorId.IsValid ? entry.RuntimeActorId.Value : "<none>");
                builder.Append(':');
                builder.Append(entry.OperationalRecipeKind);
                builder.Append(':');
                builder.Append(string.IsNullOrWhiteSpace(entry.SemanticParticipantId) ? "<none>" : entry.SemanticParticipantId);
                builder.Append(':');
                builder.Append(string.IsNullOrWhiteSpace(entry.ActorSpecId) ? "<none>" : entry.ActorSpecId);
                builder.Append(':');
                builder.Append(string.IsNullOrWhiteSpace(entry.ActorSetRef) ? "<none>" : entry.ActorSetRef);
                builder.Append(':');
                builder.Append(entry.Intent);
                builder.Append(':');
                builder.Append(entry.Classification);
            }

            return builder.ToString();
        }

        private static string ResolveSnapshotReason(List<ActorsMaterializationSpecEntry> entries)
        {
            if (entries == null || entries.Count <= 0)
            {
                return "no-spec-entries";
            }

            for (int index = 0; index < entries.Count; index += 1)
            {
                ActorMaterializationClassification classification = entries[index].Classification;
                if (classification == ActorMaterializationClassification.RuntimeOrphanProblematic ||
                    classification == ActorMaterializationClassification.InconsistentNoAutoRemediation)
                {
                    return "attention-required";
                }
            }

            return "resolved";
        }

        private void BuildPresenceLookup(ActorsPresenceSnapshot presence)
        {
            _presenceByAxis.Clear();
            ActorsPresenceRecord[] entries = presence.Entries ?? Array.Empty<ActorsPresenceRecord>();
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsPresenceRecord entry = entries[index];
                if (!entry.IsValid || _presenceByAxis.ContainsKey(entry.AxisActorId))
                {
                    continue;
                }

                _presenceByAxis.Add(entry.AxisActorId, entry);
            }
        }

        private void BuildBindingLookup(ActorsOperationalBindingSnapshot binding)
        {
            _bindingByAxis.Clear();
            ActorsOperationalBindingEntry[] entries = binding.Entries ?? Array.Empty<ActorsOperationalBindingEntry>();
            for (int index = 0; index < entries.Length; index += 1)
            {
                ActorsOperationalBindingEntry entry = entries[index];
                if (!entry.IsValid || _bindingByAxis.ContainsKey(entry.AxisActorId))
                {
                    continue;
                }

                _bindingByAxis.Add(entry.AxisActorId, entry);
            }
        }

        private ActorsPresenceRecord ResolvePresenceRecord(ActorIdentityRecord member)
        {
            if (_presenceByAxis.TryGetValue(member.AxisActorId, out ActorsPresenceRecord presenceRecord))
            {
                return presenceRecord;
            }

            return new ActorsPresenceRecord(
                member.AxisActorId,
                member.Role,
                member.Relevance,
                isExpected: true,
                isMaterialized: false,
                isInconsistent: false,
                RuntimeActorId.None,
                member.SemanticParticipantId,
                ActorPresenceStatus.ExpectedNotMaterialized,
                "presence-missing-for-axis-member");
        }

        private ActorsEnsembleSnapshot ResolveEnsembleSnapshot()
        {
            if (_ensembleService.TryGetCurrent(out ActorsEnsembleSnapshot current) && current.IsValid)
            {
                return current;
            }

            return _ensembleService.Refresh();
        }

        private ActorsPresenceSnapshot ResolvePresenceSnapshot()
        {
            if (_presenceService.TryGetCurrent(out ActorsPresenceSnapshot current) && current.IsValid)
            {
                return current;
            }

            return _presenceService.Refresh();
        }

        private static ActorsMaterializationPlanSnapshot BuildInvalidSnapshot(
            string reason,
            string ensembleSignature,
            string presenceSignature,
            string runtimeObservationSignature,
            string operationalBindingSignature)
        {
            return new ActorsMaterializationPlanSnapshot(
                ensembleSignature,
                presenceSignature,
                runtimeObservationSignature,
                operationalBindingSignature,
                string.Empty,
                Array.Empty<ActorsMaterializationSpecEntry>(),
                0,
                0,
                0,
                0,
                0,
                0,
                reason);
        }
    }
}
