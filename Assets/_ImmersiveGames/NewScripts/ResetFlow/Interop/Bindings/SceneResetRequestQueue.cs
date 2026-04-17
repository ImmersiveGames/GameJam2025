using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.ResetFlow.Interop.Bindings
{
    internal sealed class SceneResetRequestQueue
    {
        private readonly object _queueLock = new();
        private readonly Queue<ResetRequest> _resetQueue = new();
        private readonly string _sceneName;
        private readonly bool _verboseLogs;
        private bool _processingQueue;

        private const int ResetQueueBacklogWarningThreshold = 3;

        public SceneResetRequestQueue(string sceneName, bool verboseLogs)
        {
            _sceneName = sceneName ?? string.Empty;
            _verboseLogs = verboseLogs;
        }

        public Task Enqueue(string label, Func<Task> runner)
        {
            if (runner == null)
            {
                throw new ArgumentNullException(nameof(runner));
            }

            ResetRequest request;
            int position;
            bool willStartProcessing;

            lock (_queueLock)
            {
                request = new ResetRequest(label, runner);
                _resetQueue.Enqueue(request);
                position = _resetQueue.Count;

                willStartProcessing = !_processingQueue;
                if (willStartProcessing)
                {
                    _processingQueue = true;
                    _ = ProcessQueueAsync();
                }
            }

            if (position > ResetQueueBacklogWarningThreshold)
            {
                DebugUtility.LogWarning(typeof(SceneResetController),
                    $"Reset queue backlog detectado (count={position}). Possível tempestade de resets. lastLabel='{label}', scene='{_sceneName}'.");
            }

            bool queuedBehindActiveReset = !willStartProcessing;
            if (!queuedBehindActiveReset)
            {
                return request.Task;
            }

            if (position > 1)
            {
                DebugUtility.LogWarning(typeof(SceneResetController),
                    $"Reset enfileirado (posição={position}). motivo='Reset já em andamento'. label='{label}', scene='{_sceneName}'.");
            }
            else if (_verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(SceneResetController),
                    $"Reset enfileirado (posição={position}). motivo='Reset já em andamento'. label='{label}', scene='{_sceneName}'.");
            }

            return request.Task;
        }

        public void CancelPending()
        {
            lock (_queueLock)
            {
                while (_resetQueue.Count > 0)
                {
                    ResetRequest request = _resetQueue.Dequeue();
                    request.TryCancel();
                }

                _processingQueue = false;
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                ResetRequest request;

                lock (_queueLock)
                {
                    if (_resetQueue.Count == 0)
                    {
                        _processingQueue = false;
                        return;
                    }

                    request = _resetQueue.Dequeue();
                }

                try
                {
                    if (_verboseLogs)
                    {
                        DebugUtility.Log(typeof(SceneResetController),
                            $"Processando reset. label='{request.Label}', scene='{_sceneName}'.");
                    }

                    await request.Runner();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(SceneResetController),
                        $"Exception while processing reset queue item (label='{request.Label}', scene='{_sceneName}'): {ex}");
                }
                finally
                {
                    request.TryComplete();
                }
            }
        }

        private readonly struct ResetRequest
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public ResetRequest(string label, Func<Task> runner)
            {
                Label = label ?? "<null>";
                Runner = runner ?? throw new ArgumentNullException(nameof(runner));
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public string Label { get; }
            public Func<Task> Runner { get; }
            public Task Task => _tcs.Task;

            public void TryComplete()
            {
                _tcs.TrySetResult(true);
            }

            public void TryCancel()
            {
                _tcs.TrySetCanceled();
            }
        }
    }
}

