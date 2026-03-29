using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostStageControlService : IPostStageControlService
    {
        private readonly object _sync = new();
        private TaskCompletionSource<PostStageCompletionResult> _completionSource;
        private PostStageContext _currentContext;
        private bool _hasCurrentContext;
        private bool _hasCompleted;
        private bool _isActive;

        public bool IsActive
        {
            get
            {
                lock (_sync)
                {
                    return _isActive;
                }
            }
        }

        public bool HasCompleted
        {
            get
            {
                lock (_sync)
                {
                    return _hasCompleted;
                }
            }
        }

        public PostStageContext CurrentContext
        {
            get
            {
                lock (_sync)
                {
                    return _hasCurrentContext ? _currentContext : default;
                }
            }
        }

        public bool TryBegin(PostStageContext context)
        {
            lock (_sync)
            {
                if (_isActive)
                {
                    DebugUtility.LogWarning<PostStageControlService>(
                        $"[OBS][PostGame][Bridge] PostStageBeginIgnored reason='already_active' signature='{Normalize(context.Signature)}' scene='{Normalize(context.SceneName)}' frame={context.Frame} outcome='{context.Outcome}' reason='{Normalize(context.Reason)}'.");
                    return false;
                }

                _currentContext = context;
                _hasCurrentContext = true;
                _hasCompleted = false;
                _isActive = true;
                _completionSource = new TaskCompletionSource<PostStageCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                return true;
            }
        }

        public bool TryComplete(string reason = null) => TryFinish(PostStageCompletionKind.Complete, reason);

        public bool TrySkip(string reason = null) => TryFinish(PostStageCompletionKind.Skip, reason);

        public Task<PostStageCompletionResult> WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<PostStageCompletionResult>(cancellationToken);
            }

            lock (_sync)
            {
                if (_completionSource == null)
                {
                    throw new InvalidOperationException("[FATAL][PostGame] WaitForCompletionAsync chamado antes de PostStageBegin.");
                }

                return _completionSource.Task;
            }
        }

        private bool TryFinish(PostStageCompletionKind kind, string reason)
        {
            lock (_sync)
            {
                if (!_isActive || _completionSource == null || _hasCompleted)
                {
                    DebugUtility.LogWarning<PostStageControlService>(
                        $"[OBS][PostGame][Bridge] PostStageFinishIgnored kind='{kind}' reason='{Normalize(reason)}'.");
                    return false;
                }

                _hasCompleted = true;
                _isActive = false;
                return _completionSource.TrySetResult(new PostStageCompletionResult(kind, reason));
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
