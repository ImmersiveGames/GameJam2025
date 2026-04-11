using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public readonly struct GameplayPhaseRulesObjectivesSnapshot
    {
        public static GameplayPhaseRulesObjectivesSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return FromPhaseDefinitionSelectedEvent(evt, GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        internal static GameplayPhaseRulesObjectivesSnapshot FromPhaseDefinitionSelectedEvent(
            PhaseDefinitionSelectedEvent evt,
            GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRulesObjectivesSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to build the rules/objectives snapshot.");
            }

            PhaseDefinitionAsset.PhaseRulesObjectivesBlock manifest = evt.PhaseDefinitionRef.RulesObjectives;
            if (manifest == null || manifest.rules == null || manifest.objectives == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRulesObjectivesSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] PhaseDefinition phaseId='{evt.PhaseId}' has no valid rules/objectives block.");
            }

            IReadOnlyList<PhaseDefinitionAsset.PhaseRuleEntry> rules = manifest.rules;
            IReadOnlyList<PhaseDefinitionAsset.PhaseObjectiveEntry> objectives = manifest.objectives;
            int mainCount = 0;
            int auxCount = 0;
            int prototypeCount = 0;
            List<string> ruleIds = new(rules.Count);
            List<string> objectiveIds = new(objectives.Count);

            for (int i = 0; i < rules.Count; i++)
            {
                PhaseDefinitionAsset.PhaseRuleEntry entry = rules[i];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.localId))
                {
                    ruleIds.Add(entry.localId.Trim());
                }

                switch (entry.ruleKind)
                {
                    case PhaseDefinitionAsset.PhaseRuleKind.Constraint:
                        mainCount++;
                        break;
                    case PhaseDefinitionAsset.PhaseRuleKind.Gate:
                        auxCount++;
                        break;
                    case PhaseDefinitionAsset.PhaseRuleKind.Modifier:
                        prototypeCount++;
                        break;
                }
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                PhaseDefinitionAsset.PhaseObjectiveEntry entry = objectives[i];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.localId))
                {
                    objectiveIds.Add(entry.localId.Trim());
                }
            }

            string primaryObjectiveId = objectiveIds.Count > 0
                ? objectiveIds[0]
                : (ruleIds.Count > 0 ? ruleIds[0] : "<none>");

            return new GameplayPhaseRulesObjectivesSnapshot(
                phaseRuntime,
                ruleIds.Count,
                objectiveIds.Count,
                auxCount,
                prototypeCount,
                primaryObjectiveId,
                BuildRulesSignature(phaseRuntime, ruleIds.Count, mainCount, auxCount, prototypeCount, primaryObjectiveId),
                BuildObjectivesSignature(phaseRuntime, objectiveIds.Count, primaryObjectiveId),
                BuildRulesSummary(ruleIds, mainCount, auxCount, prototypeCount),
                BuildObjectivesSummary(objectiveIds));
        }

        public GameplayPhaseRulesObjectivesSnapshot(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            int ruleEntryCount,
            int objectiveEntryCount,
            int auxiliaryEntryCount,
            int prototypeEntryCount,
            string primaryObjectiveId,
            string rulesSignature,
            string objectivesSignature,
            string rulesSummary,
            string objectivesSummary)
        {
            PhaseRuntime = phaseRuntime;
            RuleEntryCount = ruleEntryCount < 0 ? 0 : ruleEntryCount;
            ObjectiveEntryCount = objectiveEntryCount < 0 ? 0 : objectiveEntryCount;
            AuxiliaryEntryCount = auxiliaryEntryCount < 0 ? 0 : auxiliaryEntryCount;
            PrototypeEntryCount = prototypeEntryCount < 0 ? 0 : prototypeEntryCount;
            PrimaryObjectiveId = Normalize(primaryObjectiveId);
            RulesSignature = Normalize(rulesSignature);
            ObjectivesSignature = Normalize(objectivesSignature);
            RulesSummary = Normalize(rulesSummary);
            ObjectivesSummary = Normalize(objectivesSummary);
        }

        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public int RuleEntryCount { get; }
        public int ObjectiveEntryCount { get; }
        public int AuxiliaryEntryCount { get; }
        public int PrototypeEntryCount { get; }
        public string PrimaryObjectiveId { get; }
        public string RulesSignature { get; }
        public string ObjectivesSignature { get; }
        public string RulesSummary { get; }
        public string ObjectivesSummary { get; }

        public bool HasRules => RuleEntryCount > 0;
        public bool HasObjectives => ObjectiveEntryCount > 0;
        public bool IsValid => PhaseRuntime.IsValid;

        public static GameplayPhaseRulesObjectivesSnapshot Empty => new(
            GameplayPhaseRuntimeSnapshot.Empty,
            0,
            0,
            0,
            0,
            "<none>",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        public override string ToString()
        {
            return $"phaseRuntime='{PhaseRuntime}', ruleEntryCount='{RuleEntryCount}', objectiveEntryCount='{ObjectiveEntryCount}', primaryObjectiveId='{PrimaryObjectiveId}', rulesSignature='{(string.IsNullOrWhiteSpace(RulesSignature) ? "<none>" : RulesSignature)}', objectivesSignature='{(string.IsNullOrWhiteSpace(ObjectivesSignature) ? "<none>" : ObjectivesSignature)}'";
        }

        private static string BuildRulesSignature(GameplayPhaseRuntimeSnapshot phaseRuntime, int totalEntries, int mainCount, int auxCount, int prototypeCount, string primaryObjectiveId)
        {
            string phaseSignature = phaseRuntime.HasPhaseRuntimeSignature ? phaseRuntime.PhaseRuntimeSignature : "<no-phase>";
            string primary = string.IsNullOrWhiteSpace(primaryObjectiveId) ? "<none>" : primaryObjectiveId;
            return $"{phaseSignature}|rules:{totalEntries}|main:{mainCount}|aux:{auxCount}|prototype:{prototypeCount}|primaryObjective:{primary}";
        }

        private static string BuildObjectivesSignature(GameplayPhaseRuntimeSnapshot phaseRuntime, int objectiveCount, string primaryObjectiveId)
        {
            string phaseSignature = phaseRuntime.HasPhaseRuntimeSignature ? phaseRuntime.PhaseRuntimeSignature : "<no-phase>";
            string primary = string.IsNullOrWhiteSpace(primaryObjectiveId) ? "<none>" : primaryObjectiveId;
            return $"{phaseSignature}|objectives:{objectiveCount}|primaryObjective:{primary}";
        }

        private static string BuildRulesSummary(IReadOnlyList<string> ruleIds, int mainCount, int auxCount, int prototypeCount)
        {
            return $"entryIds={BuildIdSummary(ruleIds)} roleCounts=[Main:{mainCount},Aux:{auxCount},Prototype:{prototypeCount}]";
        }

        private static string BuildObjectivesSummary(IReadOnlyList<string> objectiveIds)
        {
            return $"objectiveIds={BuildIdSummary(objectiveIds)}";
        }

        private static string BuildIdSummary(IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('[');

            bool first = true;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                builder.Append(id);
                first = false;
            }

            builder.Append(']');
            return builder.ToString();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IGameplayPhaseRulesObjectivesService
    {
        GameplayPhaseRulesObjectivesSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhaseRulesObjectivesSnapshot snapshot);
        bool TryGetLast(out GameplayPhaseRulesObjectivesSnapshot snapshot);
        GameplayPhaseRulesObjectivesSnapshot Update(GameplayPhaseRulesObjectivesSnapshot snapshot);
        GameplayPhaseRulesObjectivesSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseRulesObjectivesService : IGameplayPhaseRulesObjectivesService
    {
        private readonly object _sync = new();
        private GameplayPhaseRulesObjectivesSnapshot _current = GameplayPhaseRulesObjectivesSnapshot.Empty;
        private GameplayPhaseRulesObjectivesSnapshot _last = GameplayPhaseRulesObjectivesSnapshot.Empty;

        public GameplayPhaseRulesObjectivesService()
        {
            DebugUtility.LogVerbose<GameplayPhaseRulesObjectivesService>(
                "[OBS][GameplaySessionFlow][RulesObjectives] GameplayPhaseRulesObjectivesService registrado como owner dos rules/objectives da fase.");
        }

        public GameplayPhaseRulesObjectivesSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayPhaseRulesObjectivesSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return Update(GameplayPhaseRulesObjectivesSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        public GameplayPhaseRulesObjectivesSnapshot Update(GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseRulesObjectivesService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase rules/objectives snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhaseRulesObjectivesService>(
                    $"[OBS][GameplaySessionFlow][RulesObjectives] RulesObjectivesUpdated phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' ruleEntryCount='{snapshot.RuleEntryCount}' objectiveEntryCount='{snapshot.ObjectiveEntryCount}' primaryObjectiveId='{snapshot.PrimaryObjectiveId}' rulesSignature='{snapshot.RulesSignature}' objectivesSignature='{snapshot.ObjectivesSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayPhaseRulesObjectivesSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseRulesObjectivesSnapshot snapshot)
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
                _current = GameplayPhaseRulesObjectivesSnapshot.Empty;
                lastSignature = _last.RulesSignature;
            }

            DebugUtility.Log<GameplayPhaseRulesObjectivesService>(
                $"[OBS][GameplaySessionFlow][RulesObjectives] RulesObjectivesCleared keepLast='true' lastRulesSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
