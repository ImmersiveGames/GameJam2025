using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Implementação simples para P0: mantém apenas o último snapshot canônico.
    /// </summary>
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
                    snapshot.LevelId,
                    snapshot.RouteId,
                    snapshot.StyleId,
                    snapshot.ContentId,
                    snapshot.Reason,
                    persistedVersion,
                    snapshot.LevelSignature);

                DebugUtility.Log<RestartContextService>(
                    $"[OBS][Navigation] GameplayStartSnapshotUpdated levelId='{(_current.HasLevelId ? _current.LevelId.ToString() : "<none>")}' routeId='{_current.RouteId}' styleId='{_current.StyleId}' contentId='{(_current.HasContentId ? _current.ContentId : "<none>")}' v='{_current.SelectionVersion}' reason='{(string.IsNullOrWhiteSpace(_current.Reason) ? "<none>" : _current.Reason)}'.",
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


        public bool TryUpdateCurrentContentId(string contentId, string reason = null)
        {
            string normalizedContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();

            lock (_sync)
            {
                if (!_current.IsValid)
                {
                    return false;
                }

                string finalReason = string.IsNullOrWhiteSpace(normalizedReason)
                    ? _current.Reason
                    : normalizedReason;

                string updatedLevelSignature = LevelContextSignature
                    .Create(_current.LevelId, _current.RouteId, finalReason, normalizedContentId)
                    .Value;

                _current = new GameplayStartSnapshot(
                    _current.LevelId,
                    _current.RouteId,
                    _current.StyleId,
                    normalizedContentId,
                    finalReason,
                    _current.SelectionVersion,
                    updatedLevelSignature);
            }

            return true;
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
