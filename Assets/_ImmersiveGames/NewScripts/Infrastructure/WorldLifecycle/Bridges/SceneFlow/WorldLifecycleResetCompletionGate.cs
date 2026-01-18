using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Bridges.SceneFlow
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleResetCompletionGate : ISceneTransitionCompletionGate, IDisposable
    {
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _binding;

        private readonly Dictionary<string, TaskCompletionSource<string>> _pending =
            new Dictionary<string, TaskCompletionSource<string>>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _completedReasons =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly int _timeoutMs;

        private const int MaxCompletedCacheEntries = 128;

        public WorldLifecycleResetCompletionGate(int timeoutMs = 20000)
        {
            _timeoutMs = timeoutMs;

            _binding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnCompleted);
            EventBus<WorldLifecycleResetCompletedEvent>.Register(_binding);

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] WorldLifecycleResetCompletionGate registrado. timeoutMs={_timeoutMs}.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_binding);

            lock (_pending)
            {
                // Cancela awaiters pendentes (defensivo).
                foreach (KeyValuePair<string, TaskCompletionSource<string>> kv in _pending)
                {
                    kv.Value.TrySetCanceled();
                }

                _pending.Clear();
                _completedReasons.Clear();
            }
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            string signature = SceneTransitionSignatureUtil.Compute(context);

            if (string.IsNullOrEmpty(signature))
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleResetCompletionGate),
                    "[SceneFlowGate] ContextSignature vazia. Não é possível correlacionar gate; liberando sem aguardar reset.");
                return;
            }

            lock (_pending)
            {
                if (_completedReasons.ContainsKey(signature))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                        $"[SceneFlowGate] Já concluído (cached). signature='{signature}'.");
                    return;
                }
            }

            TaskCompletionSource<string> tcs;

            lock (_pending)
            {
                if (!_pending.TryGetValue(signature, out tcs))
                {
                    tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pending[signature] = tcs;
                }
            }

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (completed != tcs.Task)
            {
                lock (_pending)
                {
                    if (_pending.TryGetValue(signature, out TaskCompletionSource<string> current) && ReferenceEquals(current, tcs))
                    {
                        _pending.Remove(signature);
                    }
                }

                DebugUtility.LogWarning(typeof(WorldLifecycleResetCompletionGate),
                    $"[SceneFlowGate] Timeout aguardando WorldLifecycleResetCompletedEvent. signature='{signature}', timeoutMs={_timeoutMs}.");
                return;
            }

            string reason = await tcs.Task;

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] Concluído. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void OnCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            string signature = evt.ContextSignature ?? string.Empty;
            string reason = evt.Reason;

            if (string.IsNullOrEmpty(signature))
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleResetCompletionGate),
                    $"[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido com ContextSignature vazia. reason='{reason ?? "<null>"}'.");

                // Não faz cache nem tenta completar awaiters sem assinatura correlacionável.
                return;
            }

            TaskCompletionSource<string> tcs = null;

            lock (_pending)
            {
                PruneCompletedCacheIfNeeded();

                // Mantém o primeiro reason (estável p/ debug). Se preferir "último ganha", troque por _completedReasons[signature] = reason;
                if (!_completedReasons.ContainsKey(signature))
                {
                    _completedReasons.Add(signature, reason);
                }

                if (_pending.TryGetValue(signature, out tcs))
                {
                    _pending.Remove(signature);
                }
            }

            if (tcs != null)
            {
                tcs.TrySetResult(reason);
            }

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] WorldLifecycleResetCompletedEvent recebido. signature='{signature}', reason='{reason ?? "<null>"}'.");
        }

        private void PruneCompletedCacheIfNeeded()
        {
            if (_completedReasons.Count <= MaxCompletedCacheEntries)
            {
                return;
            }

            _completedReasons.Clear();

            DebugUtility.LogVerbose(typeof(WorldLifecycleResetCompletionGate),
                $"[SceneFlowGate] Cache de completedReasons limpo (atingiu limite > {MaxCompletedCacheEntries}).");
        }
    }
}
