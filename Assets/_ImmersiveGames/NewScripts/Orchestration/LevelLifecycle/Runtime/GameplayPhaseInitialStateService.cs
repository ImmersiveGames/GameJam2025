using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct GameplayPhaseInitialStateSnapshot
    {
        public static GameplayPhaseInitialStateSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            GameplayPhaseRuntimeSnapshot phaseRuntime = GameplayPhaseRuntimeSnapshot.FromLevelSelectedEvent(evt);
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives = ResolveRulesObjectivesSnapshot(evt, phaseRuntime);

            string seedSource = "LevelSelectedEvent";
            string initialStateSignature = BuildInitialStateSignature(
                phaseRuntime,
                rulesObjectives,
                evt.LocalContentId,
                evt.SelectionVersion);
            string initialStateSummary = BuildInitialStateSummary(
                seedSource,
                evt.LevelRef,
                evt.LocalContentId,
                evt.SelectionVersion,
                rulesObjectives);

            return new GameplayPhaseInitialStateSnapshot(
                phaseRuntime,
                rulesObjectives,
                seedSource,
                initialStateSignature,
                initialStateSummary);
        }

        public GameplayPhaseInitialStateSnapshot(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives,
            string seedSource,
            string initialStateSignature,
            string initialStateSummary)
        {
            PhaseRuntime = phaseRuntime;
            RulesObjectives = rulesObjectives;
            SeedSource = Normalize(seedSource);
            InitialStateSignature = Normalize(initialStateSignature);
            InitialStateSummary = Normalize(initialStateSummary);
        }

        public GameplayPhaseRuntimeSnapshot PhaseRuntime { get; }
        public GameplayPhaseRulesObjectivesSnapshot RulesObjectives { get; }
        public string SeedSource { get; }
        public string InitialStateSignature { get; }
        public string InitialStateSummary { get; }

        public bool IsValid => PhaseRuntime.IsValid && RulesObjectives.IsValid && !string.IsNullOrWhiteSpace(InitialStateSignature);
        public bool HasRulesObjectives => RulesObjectives.IsValid;
        public bool HasInitialStateSignature => !string.IsNullOrWhiteSpace(InitialStateSignature);

        public static GameplayPhaseInitialStateSnapshot Empty => new(
            GameplayPhaseRuntimeSnapshot.Empty,
            GameplayPhaseRulesObjectivesSnapshot.Empty,
            "<none>",
            string.Empty,
            string.Empty);

        public override string ToString()
        {
            return $"phaseRuntime='{PhaseRuntime}', seedSource='{SeedSource}', rulesObjectivesSignature='{RulesObjectives.RulesSignature}', initialStateSignature='{(string.IsNullOrWhiteSpace(InitialStateSignature) ? "<none>" : InitialStateSignature)}'";
        }

        private static GameplayPhaseRulesObjectivesSnapshot ResolveRulesObjectivesSnapshot(LevelSelectedEvent evt, GameplayPhaseRuntimeSnapshot phaseRuntime)
        {
            if (TryResolveRulesObjectivesService(out var service) &&
                service.TryGetCurrent(out GameplayPhaseRulesObjectivesSnapshot current) &&
                current.IsValid &&
                string.Equals(current.PhaseRuntime.PhaseRuntimeSignature, phaseRuntime.PhaseRuntimeSignature, StringComparison.Ordinal))
            {
                return current;
            }

            return GameplayPhaseRulesObjectivesSnapshot.FromLevelSelectedEvent(evt, phaseRuntime);
        }

        private static bool TryResolveRulesObjectivesService(out IGameplayPhaseRulesObjectivesService service)
        {
            service = null;
            return DependencyManager.Provider != null &&
                   DependencyManager.Provider.TryGetGlobal(out service) &&
                   service != null;
        }

        private static string BuildInitialStateSignature(
            GameplayPhaseRuntimeSnapshot phaseRuntime,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives,
            string localContentId,
            int selectionVersion)
        {
            string phaseSignature = phaseRuntime.HasPhaseRuntimeSignature ? phaseRuntime.PhaseRuntimeSignature : "<no-phase>";
            string normalizedContentId = string.IsNullOrWhiteSpace(localContentId) ? "<none>" : localContentId.Trim();
            return $"{phaseSignature}|initial-state|rules:{rulesObjectives.RulesSignature}|objectives:{rulesObjectives.ObjectivesSignature}|contentId:{normalizedContentId}|selectionVersion:{selectionVersion}";
        }

        private static string BuildInitialStateSummary(
            string seedSource,
            _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config.LevelDefinitionAsset levelRef,
            string localContentId,
            int selectionVersion,
            GameplayPhaseRulesObjectivesSnapshot rulesObjectives)
        {
            string levelName = levelRef != null ? levelRef.name : "<null>";
            string normalizedContentId = string.IsNullOrWhiteSpace(localContentId) ? "<none>" : localContentId.Trim();
            string normalizedSeedSource = string.IsNullOrWhiteSpace(seedSource) ? "<none>" : seedSource.Trim();

            return $"seedSource='{normalizedSeedSource}' levelRef='{levelName}' contentId='{normalizedContentId}' selectionVersion='{selectionVersion}' rulesSignature='{rulesObjectives.RulesSignature}' objectivesSignature='{rulesObjectives.ObjectivesSignature}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IGameplayPhaseInitialStateService
    {
        GameplayPhaseInitialStateSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhaseInitialStateSnapshot snapshot);
        bool TryGetLast(out GameplayPhaseInitialStateSnapshot snapshot);
        GameplayPhaseInitialStateSnapshot Update(GameplayPhaseInitialStateSnapshot snapshot);
        GameplayPhaseInitialStateSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseInitialStateService : IGameplayPhaseInitialStateService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplayPhaseInitialStateSnapshot _current = GameplayPhaseInitialStateSnapshot.Empty;
        private GameplayPhaseInitialStateSnapshot _last = GameplayPhaseInitialStateSnapshot.Empty;
        private bool _disposed;

        public GameplayPhaseInitialStateService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplayPhaseInitialStateService>(
                "[OBS][GameplaySessionFlow][InitialState] GameplayPhaseInitialStateService registrado como owner do initial state da fase.");
        }

        public GameplayPhaseInitialStateSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayPhaseInitialStateSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplayPhaseInitialStateSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplayPhaseInitialStateSnapshot Update(GameplayPhaseInitialStateSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseInitialStateService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase initial state snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhaseInitialStateService>(
                    $"[OBS][GameplaySessionFlow][InitialState] InitialStateUpdated phaseSignature='{snapshot.PhaseRuntime.PhaseRuntimeSignature}' seedSource='{snapshot.SeedSource}' rulesSignature='{snapshot.RulesObjectives.RulesSignature}' objectivesSignature='{snapshot.RulesObjectives.ObjectivesSignature}' initialStateSignature='{snapshot.InitialStateSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayPhaseInitialStateSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseInitialStateSnapshot snapshot)
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
                _current = GameplayPhaseInitialStateSnapshot.Empty;
                lastSignature = _last.InitialStateSignature;
            }

            DebugUtility.Log<GameplayPhaseInitialStateService>(
                $"[OBS][GameplaySessionFlow][InitialState] InitialStateCleared keepLast='true' lastInitialStateSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
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
