using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Result
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostRunResultService : IPostRunResultService
    {
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _disposed;

        public PostRunResultService()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
        }

        public bool HasResult { get; private set; }
        public PostRunResult Result { get; private set; } = PostRunResult.None;
        public string Reason { get; private set; } = string.Empty;

        public void Clear(string reason = null)
        {
            HasResult = false;
            Result = PostRunResult.None;
            Reason = string.Empty;

            DebugUtility.LogVerbose<PostRunResultService>(
                $"[OBS][PostRun] RunResultCleared reason='{Normalize(reason)}'.");
        }

        public bool TrySetRunOutcome(GameRunOutcome outcome, string reason = null)
        {
            if (_disposed)
            {
                return false;
            }

            PostRunResult mapped = outcome switch
            {
                GameRunOutcome.Victory => PostRunResult.Victory,
                GameRunOutcome.Defeat => PostRunResult.Defeat,
                _ => PostRunResult.None,
            };

            if (mapped == PostRunResult.None)
            {
                return false;
            }

            return TrySet(mapped, reason, "RunOutcome");
        }

        public bool TrySetExit(string reason = null)
        {
            return TrySet(PostRunResult.Exit, reason, "Exit");
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

        private bool TrySet(PostRunResult result, string reason, string source)
        {
            if (_disposed)
            {
                return false;
            }

            if (result == PostRunResult.None)
            {
                return false;
            }

            if (HasResult)
            {
                DebugUtility.LogVerbose<PostRunResultService>(
                    $"[OBS][PostRun] RunResultIgnored result='{result}' reason='{Normalize(reason)}' source='{source}' already='{Result}'.");
                return false;
            }

            HasResult = true;
            Result = result;
            Reason = Normalize(reason);

            DebugUtility.Log<PostRunResultService>(
                $"[OBS][PostRun] RunResultUpdated result='{Result}' reason='{Reason}' source='{source}'.",
                DebugUtility.Colors.Info);

            EventBus<PostRunResultUpdatedEvent>.Raise(new PostRunResultUpdatedEvent(Result, Reason));
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

