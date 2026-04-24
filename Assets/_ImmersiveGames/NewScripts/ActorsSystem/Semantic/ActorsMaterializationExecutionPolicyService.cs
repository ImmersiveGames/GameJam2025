using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    /// <summary>
    /// Operational decision projection from materialization plan.
    /// This boundary does not execute anything; it only closes the decision contract for future executors.
    /// </summary>
    public sealed class ActorsMaterializationExecutionPolicyService : IActorsMaterializationExecutionPolicyService
    {
        private readonly IActorsMaterializationPlanService _materializationPlanService;
        private ActorsMaterializationExecutionSnapshot _current = ActorsMaterializationExecutionSnapshot.Empty;

        public ActorsMaterializationExecutionPolicyService(IActorsMaterializationPlanService materializationPlanService)
        {
            _materializationPlanService = materializationPlanService ?? throw new ArgumentNullException(nameof(materializationPlanService));

            DebugUtility.Log(typeof(ActorsMaterializationExecutionPolicyService),
                "[OBS][ActorsSystem] ActorsMaterializationExecutionPolicyService registrado (projection intent/classification -> directive).",
                DebugUtility.Colors.Info);
        }

        public ActorsMaterializationExecutionSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsMaterializationExecutionSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public ActorsMaterializationExecutionSnapshot Refresh()
        {
            ActorsMaterializationPlanSnapshot plan = ResolvePlanSnapshot();
            if (!plan.IsValid)
            {
                _current = new ActorsMaterializationExecutionSnapshot(
                    plan.PlanSignature,
                    string.Empty,
                    Array.Empty<ActorsMaterializationExecutionEntry>(),
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    "no-materialization-plan");
                return _current;
            }

            ActorsMaterializationSpecEntry[] specs = plan.Entries ?? Array.Empty<ActorsMaterializationSpecEntry>();
            var entries = new List<ActorsMaterializationExecutionEntry>(specs.Length);
            int noActionStableCount = 0;
            int noActionObserveCount = 0;
            int requestMaterializeCount = 0;
            int requestRematerializeCount = 0;
            int flagInconsistentCount = 0;
            int flagRuntimeOrphanToleratedCount = 0;
            int flagRuntimeOrphanProblematicCount = 0;

            for (int index = 0; index < specs.Length; index += 1)
            {
                ActorsMaterializationSpecEntry spec = specs[index];
                if (!spec.IsValid)
                {
                    continue;
                }

                ActorMaterializationExecutionDirective directive = ResolveDirective(spec.Classification);
                if (directive == ActorMaterializationExecutionDirective.Unknown)
                {
                    continue;
                }

                string reason = BuildDirectiveReason(spec, directive);
                entries.Add(new ActorsMaterializationExecutionEntry(
                    spec.Kind,
                    spec.AxisActorId,
                    spec.RuntimeActorId,
                    spec.Role,
                    spec.OperationalRecipeKind,
                    spec.Intent,
                    spec.Classification,
                    directive,
                    spec.SemanticParticipantId,
                    spec.ActorSpecId,
                    spec.ActorSetRef,
                    reason));

                switch (directive)
                {
                    case ActorMaterializationExecutionDirective.NoActionStable:
                        noActionStableCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.NoActionObserve:
                        noActionObserveCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.RequestMaterialize:
                        requestMaterializeCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.RequestRematerialize:
                        requestRematerializeCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.FlagInconsistentNoAutoRemediation:
                        flagInconsistentCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanTolerated:
                        flagRuntimeOrphanToleratedCount += 1;
                        break;
                    case ActorMaterializationExecutionDirective.FlagRuntimeOrphanProblematic:
                        flagRuntimeOrphanProblematicCount += 1;
                        break;
                }
            }

            string executionSignature = BuildExecutionSignature(plan.PlanSignature, entries);
            string snapshotReason = (flagInconsistentCount > 0 || flagRuntimeOrphanProblematicCount > 0)
                ? "attention-required"
                : "resolved";

            _current = new ActorsMaterializationExecutionSnapshot(
                plan.PlanSignature,
                executionSignature,
                entries.ToArray(),
                noActionStableCount,
                noActionObserveCount,
                requestMaterializeCount,
                requestRematerializeCount,
                flagInconsistentCount,
                flagRuntimeOrphanToleratedCount,
                flagRuntimeOrphanProblematicCount,
                snapshotReason);

            return _current;
        }

        public void Clear(string reason = null)
        {
            _current = new ActorsMaterializationExecutionSnapshot(
                string.Empty,
                string.Empty,
                Array.Empty<ActorsMaterializationExecutionEntry>(),
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private ActorsMaterializationPlanSnapshot ResolvePlanSnapshot()
        {
            if (_materializationPlanService.TryGetCurrent(out ActorsMaterializationPlanSnapshot current) && current.IsValid)
            {
                return current;
            }

            return _materializationPlanService.Refresh();
        }

        private static ActorMaterializationExecutionDirective ResolveDirective(ActorMaterializationClassification classification)
        {
            return classification switch
            {
                ActorMaterializationClassification.StableNoAction => ActorMaterializationExecutionDirective.NoActionStable,
                ActorMaterializationClassification.ObserveNoAction => ActorMaterializationExecutionDirective.NoActionObserve,
                ActorMaterializationClassification.RequiresExecutionMaterialize => ActorMaterializationExecutionDirective.RequestMaterialize,
                ActorMaterializationClassification.RequiresExecutionRematerialize => ActorMaterializationExecutionDirective.RequestRematerialize,
                ActorMaterializationClassification.InconsistentNoAutoRemediation => ActorMaterializationExecutionDirective.FlagInconsistentNoAutoRemediation,
                ActorMaterializationClassification.RuntimeOrphanTolerated => ActorMaterializationExecutionDirective.FlagRuntimeOrphanTolerated,
                ActorMaterializationClassification.RuntimeOrphanProblematic => ActorMaterializationExecutionDirective.FlagRuntimeOrphanProblematic,
                _ => ActorMaterializationExecutionDirective.Unknown
            };
        }

        private static string BuildDirectiveReason(ActorsMaterializationSpecEntry spec, ActorMaterializationExecutionDirective directive)
        {
            return $"directive='{directive}' classification='{spec.Classification}' intent='{spec.Intent}' specReason='{(string.IsNullOrWhiteSpace(spec.Reason) ? "<none>" : spec.Reason)}'";
        }

        private static string BuildExecutionSignature(string planSignature, List<ActorsMaterializationExecutionEntry> entries)
        {
            var builder = new StringBuilder(512);
            builder.Append(string.IsNullOrWhiteSpace(planSignature) ? "<no-plan>" : planSignature.Trim());
            builder.Append("|execution-count:");
            builder.Append(entries?.Count ?? 0);

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index += 1)
                {
                    ActorsMaterializationExecutionEntry entry = entries[index];
                    builder.Append("|e:");
                    builder.Append(entry.SpecKind);
                    builder.Append(':');
                    builder.Append(entry.AxisActorId.IsValid ? entry.AxisActorId.Value : "<none>");
                    builder.Append(':');
                    builder.Append(entry.RuntimeActorId.IsValid ? entry.RuntimeActorId.Value : "<none>");
                    builder.Append(':');
                    builder.Append(entry.Role);
                    builder.Append(':');
                    builder.Append(entry.OperationalRecipeKind);
                    builder.Append(':');
                    builder.Append(string.IsNullOrWhiteSpace(entry.SemanticParticipantId) ? "<none>" : entry.SemanticParticipantId);
                    builder.Append(':');
                    builder.Append(string.IsNullOrWhiteSpace(entry.ActorSpecId) ? "<none>" : entry.ActorSpecId);
                    builder.Append(':');
                    builder.Append(string.IsNullOrWhiteSpace(entry.ActorSetRef) ? "<none>" : entry.ActorSetRef);
                    builder.Append(':');
                    builder.Append(entry.Directive);
                }
            }

            return builder.ToString();
        }
    }
}
