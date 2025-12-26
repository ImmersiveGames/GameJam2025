using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Gate de conclusão para SceneFlow:
    /// aguarda WorldLifecycleResetCompletedEvent cuja assinatura (ContextSignature) corresponda
    /// à assinatura do contexto da transição atual.
    ///
    /// Objetivo: garantir que FadeOut/Completed só aconteçam após o reset terminar (ou ser skipped).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleResetCompletionGate : ISceneTransitionCompletionGate, IDisposable
    {
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _binding;

        private readonly Dictionary<string, TaskCompletionSource<string>> _pending =
            new Dictionary<string, TaskCompletionSource<string>>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _completedReasons =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly int _timeoutMs;

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
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            string signature = SceneTransitionSignatureUtil.Compute(context);

            // Se já chegou antes (caso raro), retorna imediatamente.
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

            // Timeout defensivo para não travar o flow.
            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(_timeoutMs));
            if (completed != tcs.Task)
            {
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

            TaskCompletionSource<string> tcs = null;

            lock (_pending)
            {
                // Marca como concluído (para chamadas futuras).
                if (!_completedReasons.ContainsKey(signature))
                {
                    _completedReasons[signature] = reason;
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
    }
}
