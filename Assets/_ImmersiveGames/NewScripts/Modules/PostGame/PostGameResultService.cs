using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameResultService : IPostGameResultService
    {
        private readonly EventBinding<GameRunEndedEvent> _runEndedBinding;
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _disposed;

        public PostGameResultService()
        {
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
        }

        public bool HasResult { get; private set; }
        public PostGameResult Result { get; private set; } = PostGameResult.None;
        public string Reason { get; private set; } = string.Empty;

        public void Clear(string reason = null)
        {
            HasResult = false;
            Result = PostGameResult.None;
            Reason = string.Empty;

            DebugUtility.LogVerbose<PostGameResultService>(
                $"[OBS][PostGame] PostGameResultCleared reason='{Normalize(reason)}'.");
        }

        public bool TrySetExit(string reason = null)
        {
            if (_disposed)
            {
                return false;
            }

            HasResult = true;
            Result = PostGameResult.Exit;
            Reason = Normalize(reason);

            DebugUtility.Log<PostGameResultService>(
                $"[OBS][PostGame] PostGameResultUpdated result='{Result}' reason='{Reason}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_disposed || evt == null)
            {
                return;
            }

            PostGameResult mapped = evt.Outcome switch
            {
                GameRunOutcome.Victory => PostGameResult.Victory,
                GameRunOutcome.Defeat => PostGameResult.Defeat,
                _ => PostGameResult.None,
            };

            if (mapped == PostGameResult.None)
            {
                return;
            }

            HasResult = true;
            Result = mapped;
            Reason = Normalize(evt.Reason);

            DebugUtility.Log<PostGameResultService>(
                $"[OBS][PostGame] PostGameResultUpdated result='{Result}' reason='{Reason}'.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            Clear("GameRunStarted");
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
