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
                int candidateVersion = snapshot.SelectionVersion > 0
                    ? snapshot.SelectionVersion
                    : _selectionVersionCounter + 1;
                int persistedVersion = Math.Max(candidateVersion, _selectionVersionCounter + 1);

                _current = new GameplayStartSnapshot(
                    snapshot.LevelRef,
                    snapshot.RouteId,
                    snapshot.Reason,
                    persistedVersion,
                    snapshot.LevelSignature);

                _lastGameplayStartSnapshot = _current;
                _selectionVersionCounter = _current.SelectionVersion;

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
