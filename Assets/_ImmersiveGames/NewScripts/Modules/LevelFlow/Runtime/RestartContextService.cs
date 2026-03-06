using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class RestartContextService : IRestartContextService
    {
        private readonly object _sync = new();
        private GameplayStartSnapshot _current = GameplayStartSnapshot.Empty;

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
                int persistedVersion = snapshot.SelectionVersion > 0
                    ? snapshot.SelectionVersion
                    : _current.SelectionVersion + 1;

                _current = new GameplayStartSnapshot(
                    snapshot.LevelRef,
                    snapshot.RouteId,
                    snapshot.Reason,
                    persistedVersion,
                    snapshot.LevelSignature);

                DebugUtility.Log<RestartContextService>(
                    $"[OBS][Navigation] GameplayStartSnapshotUpdated levelRef='{(_current.HasLevelRef ? _current.LevelRef.name : "<none>")}' routeId='{_current.RouteId}' v='{_current.SelectionVersion}' reason='{(string.IsNullOrWhiteSpace(_current.Reason) ? "<none>" : _current.Reason)}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayStartSnapshot snapshot)
        {
            return TryGetLastGameplayStartSnapshot(out snapshot);
        }

        public bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            lock (_sync)
            {
                _current = GameplayStartSnapshot.Empty;
            }

            DebugUtility.Log<RestartContextService>(
                $"[OBS][Navigation] RestartContextCleared reason='{(string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim())}'.",
                DebugUtility.Colors.Info);
        }
    }
}

