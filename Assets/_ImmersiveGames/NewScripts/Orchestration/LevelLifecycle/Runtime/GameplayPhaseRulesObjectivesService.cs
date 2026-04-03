using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct GameplayPhaseRulesObjectivesSnapshot
    {
        public static GameplayPhaseRulesObjectivesSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return FromLevelSelectedEvent(evt, GameplayPhaseRuntimeSnapshot.FromLevelSelectedEvent(evt));
        }

        internal static GameplayPhaseRulesObjectivesSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt, GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (evt.LevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRulesObjectivesSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] LevelSelectedEvent requires a valid levelRef to build the rules/objectives snapshot.");
            }

            GameplayContentManifest manifest = evt.LevelRef.ContentManifest;
            if (manifest == null || manifest.Entries == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRulesObjectivesSnapshot),
                    $"[FATAL][H1][GameplaySessionFlow] LevelSelectedEvent levelRef='{evt.LevelRef.name}' has no valid gameplay content manifest for rules/objectives.");
            }

            IReadOnlyList<GameplayContentEntry> entries = manifest.Entries;
            int totalEntries = entries.Count;
            int mainCount = 0;
            int auxCount = 0;
            int prototypeCount = 0;
            List<string> ruleIds = new(totalEntries);
            List<string> objectiveIds = new();

            for (int i = 0; i < entries.Count; i++)
            {
                GameplayContentEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                string entryId = entry.EntryId;
                if (!string.IsNullOrWhiteSpace(entryId))
                {
                    ruleIds.Add(entryId);
                }

                switch (entry.Role)
                {
                    case GameplayContentEntryRole.Main:
                        mainCount++;
                        if (!string.IsNullOrWhiteSpace(entryId))
                        {
                            objectiveIds.Add(entryId);
                        }
                        break;
                    case GameplayContentEntryRole.Aux:
                        auxCount++;
                        break;
                    case GameplayContentEntryRole.Prototype:
                        prototypeCount++;
                        break;
                }
            }

            string primaryObjectiveId = objectiveIds.Count > 0
                ? objectiveIds[0]
                : (ruleIds.Count > 0 ? ruleIds[0] : "<none>");

            return new GameplayPhaseRulesObjectivesSnapshot(
                phaseRuntime,
                totalEntries,
                mainCount,
                auxCount,
                prototypeCount,
                primaryObjectiveId,
                BuildRulesSignature(phaseRuntime, totalEntries, mainCount, auxCount, prototypeCount, primaryObjectiveId),
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
        GameplayPhaseRulesObjectivesSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseRulesObjectivesService : IGameplayPhaseRulesObjectivesService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplayPhaseRulesObjectivesSnapshot _current = GameplayPhaseRulesObjectivesSnapshot.Empty;
        private GameplayPhaseRulesObjectivesSnapshot _last = GameplayPhaseRulesObjectivesSnapshot.Empty;
        private bool _disposed;

        public GameplayPhaseRulesObjectivesService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

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

        public GameplayPhaseRulesObjectivesSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplayPhaseRulesObjectivesSnapshot.FromLevelSelectedEvent(evt));
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
