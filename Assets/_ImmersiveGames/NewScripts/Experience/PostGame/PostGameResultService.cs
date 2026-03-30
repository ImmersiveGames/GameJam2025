using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameResultService : IPostGameResultService
    {
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _disposed;

        public PostGameResultService()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

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
                $"[OBS][PostGame] RunResultCleared reason='{Normalize(reason)}'.");
        }

        public bool TrySetRunOutcome(GameRunOutcome outcome, string reason = null)
        {
            if (_disposed)
            {
                return false;
            }

            PostGameResult mapped = outcome switch
            {
                GameRunOutcome.Victory => PostGameResult.Victory,
                GameRunOutcome.Defeat => PostGameResult.Defeat,
                _ => PostGameResult.None,
            };

            if (mapped == PostGameResult.None)
            {
                return false;
            }

            return TrySet(mapped, reason, "RunOutcome");
        }

        public bool TrySetExit(string reason = null)
        {
            return TrySet(PostGameResult.Exit, reason, "Exit");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
        }

        private bool TrySet(PostGameResult result, string reason, string source)
        {
            if (_disposed)
            {
                return false;
            }

            if (result == PostGameResult.None)
            {
                return false;
            }

            if (HasResult)
            {
                DebugUtility.LogVerbose<PostGameResultService>(
                    $"[OBS][PostGame] RunResultIgnored result='{result}' reason='{Normalize(reason)}' source='{source}' already='{Result}'.");
                return false;
            }

            HasResult = true;
            Result = result;
            Reason = Normalize(reason);

            DebugUtility.Log<PostGameResultService>(
                $"[OBS][PostGame] RunResultUpdated result='{Result}' reason='{Reason}' source='{source}'.",
                DebugUtility.Colors.Info);

            EventBus<PostGameResultUpdatedEvent>.Raise(new PostGameResultUpdatedEvent(Result, Reason));
            return true;
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
