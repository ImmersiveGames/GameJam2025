using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public sealed class RestartContextService : IRestartContextService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;
        private GameplayStartSnapshot _current = GameplayStartSnapshot.Empty;
        private GameplayStartSnapshot _lastGameplayStartSnapshot = GameplayStartSnapshot.Empty;
        private int _selectionVersionCounter;
        private bool _disposed;

        public RestartContextService()
        {
            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseDefinitionSelected);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);

            DebugUtility.LogVerbose<RestartContextService>(
                "[OBS][LevelLifecycle][Operational] RestartContextService registered as canonical PhaseDefinitionSelectedEvent -> GameplayStartSnapshot owner.",
                DebugUtility.Colors.Info);
        }

        public GameplayStartSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayStartSnapshot RegisterGameplayStart(GameplayStartSnapshot snapshot)
        {
            return UpdateGameplayStartSnapshot(snapshot);
        }

        public GameplayStartSnapshot RegisterGameplayStart(PhaseDefinitionSelectedEvent evt)
        {
            return UpdateGameplayStartSnapshot(GameplayStartSnapshot.FromPhaseDefinitionSelectedEvent(evt));
        }

        public GameplayStartSnapshot UpdateGameplayStartSnapshot(GameplayStartSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.HasPhaseDefinitionRef || !snapshot.MacroRouteId.IsValid || snapshot.MacroRouteRef == null)
                {
                    string invalidReason = !snapshot.HasPhaseDefinitionRef && !snapshot.MacroRouteId.IsValid && snapshot.MacroRouteRef == null
                        ? "missing-phaseRef-and-invalid-routeId-and-routeRef"
                        : (!snapshot.HasPhaseDefinitionRef
                            ? "missing-phaseRef"
                            : (!snapshot.MacroRouteId.IsValid && snapshot.MacroRouteRef != null
                                ? "invalid-routeId"
                                : "missing-routeRef"));

                    DebugUtility.Log<RestartContextService>(
                        $"[WARN][LevelFlow] Ignored invalid GameplayStartSnapshot. phaseRef='{(snapshot.HasPhaseDefinitionRef ? snapshot.PhaseDefinitionRef.name : "<null>")}' routeId='{snapshot.MacroRouteId}' routeRef='{(snapshot.MacroRouteRef != null ? snapshot.MacroRouteRef.name : "<null>")}' reason='{invalidReason}'.",
                        DebugUtility.Colors.Warning);

                    return _current;
                }

                int incoming = snapshot.SelectionVersion;
                int next;

                if (incoming <= 0)
                {
                    next = _selectionVersionCounter + 1;
                }
                else if (incoming < _selectionVersionCounter)
                {
                    next = _selectionVersionCounter + 1;
                }
                else
                {
                    if (incoming == _selectionVersionCounter)
                    {
                        DebugUtility.Log<RestartContextService>(
                            $"[OBS][LevelFlow] GameplayStartSnapshotWrite dedupe reason='same_selection_version' v='{incoming}' routeId='{snapshot.MacroRouteId}' phaseRef='{(snapshot.HasPhaseDefinitionRef ? snapshot.PhaseDefinitionRef.name : "<none>")}' localContentId='{snapshot.LocalContentId}'",
                            DebugUtility.Colors.Info);
                    }

                    next = incoming;
                }

                _current = new GameplayStartSnapshot(
                    snapshot.PhaseDefinitionRef,
                    snapshot.MacroRouteId,
                    snapshot.MacroRouteRef,
                    snapshot.LocalContentId,
                    snapshot.Reason,
                    next,
                    snapshot.LevelSignature);

                _lastGameplayStartSnapshot = _current;
                _selectionVersionCounter = Math.Max(_selectionVersionCounter, next);

                LogSnapshotUpdated(_current);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayStartSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
        }

        public bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _lastGameplayStartSnapshot;
                return _lastGameplayStartSnapshot.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            int lastSelectionVersion;

            lock (_sync)
            {
                _current = GameplayStartSnapshot.Empty;
                lastSelectionVersion = _lastGameplayStartSnapshot.SelectionVersion;
            }

            LogContextCleared(lastSelectionVersion, reason);
        }

        private static void LogSnapshotUpdated(GameplayStartSnapshot snapshot)
        {
            DebugUtility.Log<RestartContextService>(
                $"[OBS][LevelFlow] GameplayStartSnapshotUpdated phaseRef='{(snapshot.HasPhaseDefinitionRef ? snapshot.PhaseDefinitionRef.name : "<none>")}' routeId='{snapshot.MacroRouteId}' contentId='{snapshot.LocalContentId}' v='{snapshot.SelectionVersion}' reason='{(string.IsNullOrWhiteSpace(snapshot.Reason) ? "<none>" : snapshot.Reason)}' levelSignature='{(string.IsNullOrWhiteSpace(snapshot.LevelSignature) ? "<none>" : snapshot.LevelSignature)}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogContextCleared(int lastSelectionVersion, string reason)
        {
            DebugUtility.Log<RestartContextService>(
                $"[OBS][LevelFlow] RestartContextCleared keepLast='true' lastSelectionV='{lastSelectionVersion}' reason='{(string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim())}'.",
                DebugUtility.Colors.Info);
        }

        private void OnPhaseDefinitionSelected(PhaseDefinitionSelectedEvent evt)
        {
            RegisterGameplayStart(evt);
        }
    }
}
