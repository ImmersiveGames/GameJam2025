using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class RestartContextService : IRestartContextService
    {
        private readonly object _sync = new();
        private GameplayStartSnapshot _current = GameplayStartSnapshot.Empty;
        private GameplayStartSnapshot _lastGameplayStartSnapshot = GameplayStartSnapshot.Empty;
        private int _selectionVersionCounter;

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

        public GameplayStartSnapshot UpdateGameplayStartSnapshot(GameplayStartSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.HasLevelRef || !snapshot.RouteId.IsValid)
                {
                    string invalidReason = !snapshot.HasLevelRef && !snapshot.RouteId.IsValid
                        ? "missing-levelRef-and-invalid-routeId"
                        : (!snapshot.HasLevelRef ? "missing-levelRef" : "invalid-routeId");

                    DebugUtility.Log<RestartContextService>(
                        $"[WARN][LevelFlow] Ignored invalid GameplayStartSnapshot. levelRef='{(snapshot.HasLevelRef ? snapshot.LevelRef.name : "<null>")}' routeId='{snapshot.RouteId}' reason='{invalidReason}'.",
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
                            $"[OBS][LevelFlow] GameplayStartSnapshotWrite dedupe reason='same_selection_version' v='{incoming}' routeId='{snapshot.RouteId}' levelRef='{(snapshot.HasLevelRef ? snapshot.LevelRef.name : "<null>")}'",
                            DebugUtility.Colors.Info);
                    }

                    next = incoming;
                }

                _current = new GameplayStartSnapshot(
                    snapshot.LevelRef,
                    snapshot.RouteId,
                    snapshot.Reason,
                    next,
                    snapshot.LevelSignature);

                _lastGameplayStartSnapshot = _current;
                _selectionVersionCounter = Math.Max(_selectionVersionCounter, next);

                DebugUtility.Log<RestartContextService>(
                    $"[OBS][Navigation] GameplayStartSnapshotUpdated levelRef='{(_current.HasLevelRef ? _current.LevelRef.name : "<none>")}' routeId='{_current.RouteId}' v='{_current.SelectionVersion}' reason='{(string.IsNullOrWhiteSpace(_current.Reason) ? "<none>" : _current.Reason)}'.",
                    DebugUtility.Colors.Info);

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

            DebugUtility.Log<RestartContextService>(
                $"[OBS][Navigation] RestartContextCleared keepLast='true' lastSelectionV='{lastSelectionVersion}' reason='{(string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim())}'.",
                DebugUtility.Colors.Info);
        }
    }
}


