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
            lock (_sync)
            {
                int nextVersion = _current.SelectionVersion + 1;
                _current = new GameplayStartSnapshot(
                    snapshot.LevelId,
                    snapshot.RouteId,
                    snapshot.StyleId,
                    snapshot.ContentId,
                    snapshot.Reason,
                    nextVersion,
                    snapshot.ContextSignature);

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
